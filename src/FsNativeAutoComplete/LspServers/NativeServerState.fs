/// Native Server State Management
/// Manages native project state using FNCS (F# Native Compiler Services)
/// for .fidproj projects and .fsnx script files.
///
/// This is the NATIVE-FIRST path for Fidelity projects.
/// Standard F# projects (.fsproj) still use AdaptiveState.
namespace FsNativeAutoComplete.Lsp

open System
open System.IO
open System.Collections.Generic
open System.Collections.Concurrent
open FsNativeAutoComplete.Core
open FsNativeAutoComplete.Core.ProjectKind
open FsNativeAutoComplete.Core.FidprojLoader
open FsNativeAutoComplete.Core.NativeCompilerServiceInterface
open FsNativeAutoComplete.Core.FsniDirectives
open FSharp.UMX
open Ionide.LanguageServerProtocol.Types
open FsNativeAutoComplete.LspHelpers
open FsNativeAutoComplete.Utils

// =============================================================================
// Native Project Types
// =============================================================================

/// A loaded native project with its options and check results
[<CustomEquality; NoComparison>]
type LoadedNativeProject = {
    /// Parsed .fidproj options
    Options: FidprojOptions
    /// Cached check results per file
    FileResults: Map<string, NativeCheckResult>
    /// Last modified time of project file
    LastModified: DateTime
}
with
    interface IEquatable<LoadedNativeProject> with
        member x.Equals(other) =
            x.Options.ProjectPath = other.Options.ProjectPath

    override x.GetHashCode() = x.Options.ProjectPath.GetHashCode()

    override x.Equals(other: obj) =
        match other with
        | :? LoadedNativeProject as other -> (x :> IEquatable<_>).Equals other
        | _ -> false

    member x.ProjectFileName = x.Options.ProjectPath

/// Volatile native file (in-memory content)
type VolatileNativeFile = {
    /// File path
    FilePath: string
    /// Current source text
    Source: string
    /// Version for change tracking
    Version: int
    /// Last check result (if available)
    LastCheckResult: NativeCheckResult option
}

/// A loaded native script (.fsnx) with its parsed options
type LoadedNativeScript = {
    /// Parsed script options from FSNI directives
    Options: FsnxScriptOptions
    /// Current source (may be volatile if open in editor)
    Source: string
    /// Cached check result
    LastCheckResult: NativeCheckResult option
    /// Last modified time of script file
    LastModified: DateTime
}

// =============================================================================
// Native State Management
// =============================================================================

/// State manager for native (FNCS-based) projects
type NativeState(_lspClient: FSharpLspClient) =

    // -------------------------------------------------------------------------
    // Internal State
    // -------------------------------------------------------------------------

    /// The native checker (FNCS wrapper)
    let checker = FSharpNativeChecker()

    /// Loaded native projects by project path
    let loadedProjects = ConcurrentDictionary<string, LoadedNativeProject>()

    /// Cached project-level check results (shared across all files in a project)
    let projectCheckResults = ConcurrentDictionary<string, NativeCheckResult>()

    /// Loaded native scripts by script path
    let loadedScripts = ConcurrentDictionary<string, LoadedNativeScript>()

    /// Volatile files (open in editor)
    let volatileFiles = ConcurrentDictionary<string, VolatileNativeFile>()

    /// Map from source file to owning project
    let fileToProject = ConcurrentDictionary<string, FidprojOptions>()

    // -------------------------------------------------------------------------
    // Project Loading
    // -------------------------------------------------------------------------

    /// Load a native project from a .fidproj file
    member _.LoadProject(fidprojPath: string) : Result<LoadedNativeProject, string> =
        match load fidprojPath with
        | Error e -> Error e
        | Ok options ->
            let project = {
                Options = options
                FileResults = Map.empty
                LastModified = File.GetLastWriteTimeUtc(fidprojPath)
            }

            // Register the project
            loadedProjects.[fidprojPath] <- project

            // Register source files
            for sourceFile in options.ResolvedSourceFiles do
                fileToProject.[sourceFile] <- options

            Ok project

    /// Unload a project
    member _.UnloadProject(fidprojPath: string) =
        match loadedProjects.TryRemove(fidprojPath) with
        | true, project ->
            // Unregister source files
            for sourceFile in project.Options.ResolvedSourceFiles do
                fileToProject.TryRemove(sourceFile) |> ignore
        | false, _ -> ()

    /// Find which project a source file belongs to
    member _.GetProjectForFile(filePath: string) : FidprojOptions option =
        let normalizedPath = Path.GetFullPath(filePath)

        // First check the direct lookup (file explicitly listed in project)
        match fileToProject.TryGetValue(normalizedPath) with
        | true, opts -> Some opts
        | false, _ ->
            // Check if file is in any loaded project's directory
            let fileDir = Path.GetDirectoryName(normalizedPath)
            loadedProjects.Values
            |> Seq.tryFind (fun project ->
                String.Equals(project.Options.ProjectDirectory, fileDir, StringComparison.OrdinalIgnoreCase))
            |> Option.map (fun project -> project.Options)

    // -------------------------------------------------------------------------
    // Script Loading
    // -------------------------------------------------------------------------

    /// Load a native script from a .fsnx file
    member _.LoadScript(scriptPath: string) : Result<LoadedNativeScript, string> =
        match loadScript scriptPath with
        | Error e -> Error e
        | Ok options ->
            let script = {
                Options = options
                Source = options.Source
                LastCheckResult = None
                LastModified = File.GetLastWriteTimeUtc(scriptPath)
            }

            let normalizedPath = Path.GetFullPath(scriptPath)
            loadedScripts.[normalizedPath] <- script
            Ok script

    /// Unload a script
    member _.UnloadScript(scriptPath: string) =
        let normalizedPath = Path.GetFullPath(scriptPath)
        loadedScripts.TryRemove(normalizedPath) |> ignore

    /// Get a loaded script
    member _.GetScript(scriptPath: string) : LoadedNativeScript option =
        let normalizedPath = Path.GetFullPath(scriptPath)
        match loadedScripts.TryGetValue(normalizedPath) with
        | true, script -> Some script
        | false, _ -> None

    /// Update a script's source (when edited)
    member _.UpdateScript(scriptPath: string, newSource: string) =
        let normalizedPath = Path.GetFullPath(scriptPath)
        match loadedScripts.TryGetValue(normalizedPath) with
        | true, script ->
            // Re-parse the directives with the new source
            let options = parseScript scriptPath newSource
            loadedScripts.[normalizedPath] <- {
                script with
                    Options = options
                    Source = newSource
                    LastCheckResult = None  // Invalidate cached result
            }
        | false, _ -> ()

    /// Check if a file is a native script
    member _.IsScript(filePath: string) : bool =
        isNativeScript filePath

    // -------------------------------------------------------------------------
    // Document Management
    // -------------------------------------------------------------------------

    /// Open a document
    member _.OpenDocument(filePath: string, source: string, version: int) =
        let normalizedPath = Path.GetFullPath(filePath)
        volatileFiles.[normalizedPath] <- {
            FilePath = normalizedPath
            Source = source
            Version = version
            LastCheckResult = None
        }

    /// Update a document
    member this.UpdateDocument(filePath: string, source: string, version: int) =
        let normalizedPath = Path.GetFullPath(filePath)
        volatileFiles.[normalizedPath] <- {
            FilePath = normalizedPath
            Source = source
            Version = version
            LastCheckResult = None  // Invalidate cached result
        }

        // Also invalidate project-level cache if this file belongs to a project
        match this.GetProjectForFile(normalizedPath) with
        | Some projectOptions ->
            this.InvalidateProjectCache(projectOptions.ProjectPath)
        | None -> ()

    /// Close a document
    member _.CloseDocument(filePath: string) =
        let normalizedPath = Path.GetFullPath(filePath)
        volatileFiles.TryRemove(normalizedPath) |> ignore

    /// Get document source (volatile, script, or from disk)
    member _.GetSource(filePath: string) : string option =
        let normalizedPath = Path.GetFullPath(filePath)
        // Check volatile files first (open in editor)
        match volatileFiles.TryGetValue(normalizedPath) with
        | true, vf -> Some vf.Source
        | false, _ ->
            // Check if it's a loaded script
            match loadedScripts.TryGetValue(normalizedPath) with
            | true, script -> Some script.Source
            | false, _ ->
                // Fall back to disk
                if File.Exists(normalizedPath) then
                    Some (File.ReadAllText(normalizedPath))
                else
                    None

    // -------------------------------------------------------------------------
    // Type Checking
    // -------------------------------------------------------------------------

    /// Check a single file (for standalone files or scripts)
    /// For project files, use CheckFileWithProject instead
    member this.CheckFile(filePath: string) : Result<NativeCheckResult, string> =
        let normalizedPath = Path.GetFullPath(filePath)

        // First check if this file belongs to a project
        match this.GetProjectForFile(normalizedPath) with
        | Some projectOptions ->
            // File belongs to a project - check the whole project
            this.CheckFileWithProject(normalizedPath, projectOptions)
        | None ->
            // Standalone file - check in isolation
            match this.GetSource(normalizedPath) with
            | None -> Error $"File not found: {filePath}"
            | Some source ->
                let result = checker.ParseAndCheckFile(normalizedPath, source)

                // Cache the result
                match volatileFiles.TryGetValue(normalizedPath) with
                | true, vf ->
                    volatileFiles.[normalizedPath] <- { vf with LastCheckResult = Some result }
                | false, _ -> ()

                Ok result

    /// Check a file that belongs to a project (checks all project files with shared environment)
    member this.CheckFileWithProject(filePath: string, projectOptions: FidprojOptions) : Result<NativeCheckResult, string> =
        let projectPath = projectOptions.ProjectPath

        // Get all source files in dependency order (Alloy first, then project sources)
        let allSources = getProjectSources projectOptions

        // Read all sources (use volatile content if available)
        let sourceContents =
            allSources
            |> List.choose (fun sourcePath ->
                match this.GetSource(sourcePath) with
                | Some content -> Some (sourcePath, content)
                | None ->
                    // Log but continue - file might not exist yet
                    printfn "[NativeState] Warning: Could not read source: %s" sourcePath
                    None)

        if List.isEmpty sourceContents then
            Error "No source files found for project"
        else
            // Check all files together with shared type environment
            let result = checker.ParseAndCheckProject(sourceContents)

            // Cache the project-level result
            projectCheckResults.[projectPath] <- result

            // Also cache for individual file lookups
            let normalizedPath = Path.GetFullPath(filePath)
            match volatileFiles.TryGetValue(normalizedPath) with
            | true, vf ->
                volatileFiles.[normalizedPath] <- { vf with LastCheckResult = Some result }
            | false, _ -> ()

            Ok result

    /// Invalidate cached project result (e.g., when a file changes)
    member _.InvalidateProjectCache(projectPath: string) =
        projectCheckResults.TryRemove(projectPath) |> ignore

    /// Get cached project check result
    member _.GetProjectCheckResult(projectPath: string) : NativeCheckResult option =
        match projectCheckResults.TryGetValue(projectPath) with
        | true, result -> Some result
        | false, _ -> None

    /// Check all files in a project (returns the combined result)
    member this.CheckProject(project: LoadedNativeProject) : NativeCheckResult =
        let sources = getProjectSources project.Options

        let sourceContents =
            sources
            |> List.choose (fun filePath ->
                match this.GetSource(filePath) with
                | Some source -> Some (filePath, source)
                | None -> None)

        checker.ParseAndCheckProject(sourceContents)

    /// Get cached check result for a file (checks volatile files first, then project cache)
    member this.GetCachedCheckResult(filePath: string) : NativeCheckResult option =
        let normalizedPath = Path.GetFullPath(filePath)

        // Check volatile files first (for open documents)
        match volatileFiles.TryGetValue(normalizedPath) with
        | true, vf when vf.LastCheckResult.IsSome -> vf.LastCheckResult
        | _ ->
            // Check if this file belongs to a project with cached results
            match this.GetProjectForFile(normalizedPath) with
            | Some projectOptions ->
                this.GetProjectCheckResult(projectOptions.ProjectPath)
            | None -> None

    // -------------------------------------------------------------------------
    // LSP Feature Support
    // -------------------------------------------------------------------------

    /// Get hover information at a position
    member this.GetHoverInfo(filePath: string, line: int, col: int) : NativeHoverInfo option =
        match this.GetCachedCheckResult(filePath) with
        | Some result -> checker.GetHoverInfo(result, filePath, line, col)
        | None ->
            // Check the file first
            match this.CheckFile(filePath) with
            | Ok result -> checker.GetHoverInfo(result, filePath, line, col)
            | Error _ -> None

    /// Get completions at a position
    member this.GetCompletions(filePath: string, line: int, col: int) : NativeCompletionItem list =
        match this.GetCachedCheckResult(filePath) with
        | Some result -> checker.GetCompletions(result, line, col)
        | None ->
            match this.CheckFile(filePath) with
            | Ok result -> checker.GetCompletions(result, line, col)
            | Error _ -> []

    /// Get diagnostics for a file
    member this.GetDiagnostics(filePath: string) : NativeDiagnostic list =
        match this.GetCachedCheckResult(filePath) with
        | Some result -> checker.GetDiagnostics(result)
        | None ->
            match this.CheckFile(filePath) with
            | Ok result -> checker.GetDiagnostics(result)
            | Error msg ->
                [{ Severity = LspDiagnosticSeverity.Error
                   Code = "FS0001"
                   Message = msg
                   Range = { Start = { Line = 0; Column = 0 }
                             End = { Line = 0; Column = 0 } }
                   Source = "fsnative" }]

    /// Get definition location at a position
    member this.GetDefinition(filePath: string, line: int, col: int) : NativeDefinitionResult option =
        match this.GetCachedCheckResult(filePath) with
        | Some result -> checker.GetDefinition(result, line, col)
        | None ->
            match this.CheckFile(filePath) with
            | Ok result -> checker.GetDefinition(result, line, col)
            | Error _ -> None

    // -------------------------------------------------------------------------
    // Workspace Discovery
    // -------------------------------------------------------------------------

    /// Find all native projects in a directory
    member _.DiscoverProjects(rootDirectory: string) : string list =
        findAllProjects rootDirectory

    /// Find all native scripts in a directory
    member _.DiscoverScripts(rootDirectory: string) : string list =
        if Directory.Exists(rootDirectory) then
            Directory.GetFiles(rootDirectory, "*.fsnx", SearchOption.AllDirectories)
            |> Array.toList
        else
            []

    /// Check if a file is relevant for native workspace
    member _.IsNativeFile(filePath: string) : bool =
        isNativeWorkspaceFile filePath || isNativeScript filePath

    /// Check if this is a native project kind
    member _.IsNativeProject(filePath: string) : bool =
        detectFromPath filePath = ProjectKind.Native

    // -------------------------------------------------------------------------
    // State Access
    // -------------------------------------------------------------------------

    /// Get all loaded projects
    member _.GetLoadedProjects() : LoadedNativeProject list =
        loadedProjects.Values |> Seq.toList

    /// Get all loaded scripts
    member _.GetLoadedScripts() : LoadedNativeScript list =
        loadedScripts.Values |> Seq.toList

    /// Get all volatile files
    member _.GetVolatileFiles() : VolatileNativeFile list =
        volatileFiles.Values |> Seq.toList

// =============================================================================
// LSP Diagnostic Conversion
// =============================================================================

module NativeDiagnostics =

    /// Convert native diagnostics to LSP diagnostics
    let toLspDiagnostics (diagnostics: NativeDiagnostic list) : Diagnostic array =
        diagnostics
        |> List.map (fun d ->
            { Range =
                { Start = { Line = uint32 d.Range.Start.Line
                            Character = uint32 d.Range.Start.Column }
                  End = { Line = uint32 d.Range.End.Line
                          Character = uint32 d.Range.End.Column } }
              Severity = Some (enum<DiagnosticSeverity> (int d.Severity))
              Code = Some (U2.C2 d.Code)
              CodeDescription = None
              Source = Some d.Source
              Message = d.Message
              RelatedInformation = None
              Tags = None
              Data = None })
        |> List.toArray

    /// Convert native hover to LSP hover
    let toLspHover (hover: NativeHoverInfo) : Hover =
        { Contents =
            U3.C1 { Kind = MarkupKind.Markdown
                    Value = hover.Contents }
          Range =
            hover.Range
            |> Option.map (fun r ->
                { Start = { Line = uint32 r.Start.Line
                            Character = uint32 r.Start.Column }
                  End = { Line = uint32 r.End.Line
                          Character = uint32 r.End.Column } }) }

    /// Convert native completion items to LSP completion items
    let toLspCompletionItems (items: NativeCompletionItem list) : CompletionItem array =
        items
        |> List.map (fun item ->
            { Label = item.Label
              LabelDetails = None
              Kind = Some (enum<CompletionItemKind> item.Kind)
              Tags = None
              Detail = item.Detail
              Documentation = item.Documentation |> Option.map (fun d -> U2.C1 d)
              Deprecated = None
              Preselect = None
              SortText = None
              FilterText = None
              InsertText = item.InsertText
              InsertTextFormat = None
              InsertTextMode = None
              TextEdit = None
              TextEditText = None
              AdditionalTextEdits = None
              CommitCharacters = None
              Command = None
              Data = None })
        |> List.toArray

    /// Convert native definition result to LSP location
    let toLspLocation (result: NativeDefinitionResult) : Location =
        { Uri = Path.FilePathToUri result.FilePath
          Range = { Start = { Line = uint32 result.Range.Start.Line
                              Character = uint32 result.Range.Start.Column }
                    End = { Line = uint32 result.Range.End.Line
                            Character = uint32 result.Range.End.Column } } }

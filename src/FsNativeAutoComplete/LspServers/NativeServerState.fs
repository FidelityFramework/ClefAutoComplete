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
open FSharp.UMX
open Ionide.LanguageServerProtocol.Types

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
        match fileToProject.TryGetValue(normalizedPath) with
        | true, opts -> Some opts
        | false, _ -> None

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
    member _.UpdateDocument(filePath: string, source: string, version: int) =
        let normalizedPath = Path.GetFullPath(filePath)
        volatileFiles.[normalizedPath] <- {
            FilePath = normalizedPath
            Source = source
            Version = version
            LastCheckResult = None  // Invalidate cached result
        }

    /// Close a document
    member _.CloseDocument(filePath: string) =
        let normalizedPath = Path.GetFullPath(filePath)
        volatileFiles.TryRemove(normalizedPath) |> ignore

    /// Get document source (volatile or from disk)
    member _.GetSource(filePath: string) : string option =
        let normalizedPath = Path.GetFullPath(filePath)
        match volatileFiles.TryGetValue(normalizedPath) with
        | true, vf -> Some vf.Source
        | false, _ ->
            if File.Exists(normalizedPath) then
                Some (File.ReadAllText(normalizedPath))
            else
                None

    // -------------------------------------------------------------------------
    // Type Checking
    // -------------------------------------------------------------------------

    /// Check a single file
    member this.CheckFile(filePath: string) : Result<NativeCheckResult, string> =
        match this.GetSource(filePath) with
        | None -> Error $"File not found: {filePath}"
        | Some source ->
            let result = checker.ParseAndCheckFile(filePath, source)

            // Cache the result
            let normalizedPath = Path.GetFullPath(filePath)
            match volatileFiles.TryGetValue(normalizedPath) with
            | true, vf ->
                volatileFiles.[normalizedPath] <- { vf with LastCheckResult = Some result }
            | false, _ -> ()

            Ok result

    /// Check all files in a project
    member this.CheckProject(project: LoadedNativeProject) : NativeCheckResult list =
        let sources = getProjectSources project.Options

        sources
        |> List.map (fun filePath ->
            match this.GetSource(filePath) with
            | Some source -> (filePath, source)
            | None -> (filePath, ""))
        |> checker.ParseAndCheckFiles

    /// Get cached check result for a file
    member _.GetCachedCheckResult(filePath: string) : NativeCheckResult option =
        let normalizedPath = Path.GetFullPath(filePath)
        match volatileFiles.TryGetValue(normalizedPath) with
        | true, vf -> vf.LastCheckResult
        | false, _ -> None

    // -------------------------------------------------------------------------
    // LSP Feature Support
    // -------------------------------------------------------------------------

    /// Get hover information at a position
    member this.GetHoverInfo(filePath: string, line: int, col: int) : NativeHoverInfo option =
        match this.GetCachedCheckResult(filePath) with
        | Some result -> checker.GetHoverInfo(result, line, col)
        | None ->
            // Check the file first
            match this.CheckFile(filePath) with
            | Ok result -> checker.GetHoverInfo(result, line, col)
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

    // -------------------------------------------------------------------------
    // Workspace Discovery
    // -------------------------------------------------------------------------

    /// Find all native projects in a directory
    member _.DiscoverProjects(rootDirectory: string) : string list =
        findAllProjects rootDirectory

    /// Check if a file is relevant for native workspace
    member _.IsNativeFile(filePath: string) : bool =
        isNativeWorkspaceFile filePath

    /// Check if this is a native project kind
    member _.IsNativeProject(filePath: string) : bool =
        detectFromPath filePath = ProjectKind.Native

    // -------------------------------------------------------------------------
    // State Access
    // -------------------------------------------------------------------------

    /// Get all loaded projects
    member _.GetLoadedProjects() : LoadedNativeProject list =
        loadedProjects.Values |> Seq.toList

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

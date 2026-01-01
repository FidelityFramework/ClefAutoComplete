/// FSNI (F# Native Interactive) Directive Parser
/// Parses directives in .fsnx script files for native compilation settings.
///
/// Supported directives:
///   #target "thumbv8m.main-none-eabihf"  - Set target triple
///   #memory_model stack_only             - Set memory model
///   #arena 4096                          - Set arena size
///   #require "alloy"                     - Require a dependency
///   #include "path/to/file.fs"           - Include another source file
///   #load "path/to/script.fsnx"          - Load another script
///   #platform "stm32l5"                  - Set platform template
module FsNativeAutoComplete.Core.FsniDirectives

open System
open System.IO
open System.Text.RegularExpressions

// =============================================================================
// Directive Types
// =============================================================================

/// Memory model directive values
[<RequireQualifiedAccess>]
type MemoryModelDirective =
    | StackOnly
    | StaticPools
    | Arena
    | Standard

/// A parsed FSNI directive
[<RequireQualifiedAccess>]
type FsniDirective =
    /// #target "triple"
    | Target of triple: string
    /// #memory_model model
    | MemoryModel of model: MemoryModelDirective
    /// #arena size
    | Arena of size: int
    /// #require "dependency"
    | Require of dependency: string
    /// #include "path"
    | Include of path: string
    /// #load "path"
    | Load of path: string
    /// #platform "template"
    | Platform of template: string
    /// #max_stack size
    | MaxStack of size: int
    /// #heap size
    | Heap of size: int

/// Result of parsing a script file
type ScriptParseResult = {
    /// All parsed directives
    Directives: FsniDirective list
    /// Line numbers where directives were found
    DirectiveLines: int list
    /// The source code with directives removed (for type checking)
    SourceWithoutDirectives: string
    /// Any parse errors
    Errors: (int * string) list
}

// =============================================================================
// Directive Parsing
// =============================================================================

let private directivePattern = Regex(@"^#(\w+)\s*(.*)$", RegexOptions.Compiled)
let private quotedStringPattern = Regex(@"""([^""]+)""", RegexOptions.Compiled)

let private parseMemoryModel (value: string) : MemoryModelDirective option =
    match value.ToLowerInvariant().Trim() with
    | "stack_only" | "stackonly" -> Some MemoryModelDirective.StackOnly
    | "static_pools" | "staticpools" -> Some MemoryModelDirective.StaticPools
    | "arena" -> Some MemoryModelDirective.Arena
    | "standard" -> Some MemoryModelDirective.Standard
    | _ -> None

let private extractQuotedString (s: string) : string option =
    let m = quotedStringPattern.Match(s)
    if m.Success then Some m.Groups.[1].Value
    else None

let private parseInt (s: string) : int option =
    match Int32.TryParse(s.Trim()) with
    | true, v -> Some v
    | false, _ -> None

/// Parse a single directive line
let parseDirectiveLine (line: string) : Result<FsniDirective option, string> =
    let trimmed = line.Trim()

    // Skip empty lines and regular comments
    if String.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith("//") then
        Ok None
    // Check if it's a directive
    elif trimmed.StartsWith("#") then
        let m = directivePattern.Match(trimmed)
        if m.Success then
            let directive = m.Groups.[1].Value.ToLowerInvariant()
            let args = m.Groups.[2].Value.Trim()

            match directive with
            | "target" ->
                match extractQuotedString args with
                | Some triple -> Ok (Some (FsniDirective.Target triple))
                | None -> Error $"#target requires a quoted string argument"

            | "memory_model" ->
                match parseMemoryModel args with
                | Some model -> Ok (Some (FsniDirective.MemoryModel model))
                | None -> Error $"Invalid memory model: {args}. Use: stack_only, static_pools, arena, or standard"

            | "arena" ->
                match parseInt args with
                | Some size -> Ok (Some (FsniDirective.Arena size))
                | None -> Error $"#arena requires an integer size"

            | "require" ->
                match extractQuotedString args with
                | Some dep -> Ok (Some (FsniDirective.Require dep))
                | None -> Error $"#require requires a quoted string argument"

            | "include" ->
                match extractQuotedString args with
                | Some path -> Ok (Some (FsniDirective.Include path))
                | None -> Error $"#include requires a quoted string argument"

            | "load" ->
                match extractQuotedString args with
                | Some path -> Ok (Some (FsniDirective.Load path))
                | None -> Error $"#load requires a quoted string argument"

            | "platform" ->
                match extractQuotedString args with
                | Some template -> Ok (Some (FsniDirective.Platform template))
                | None -> Error $"#platform requires a quoted string argument"

            | "max_stack" ->
                match parseInt args with
                | Some size -> Ok (Some (FsniDirective.MaxStack size))
                | None -> Error $"#max_stack requires an integer size"

            | "heap" ->
                match parseInt args with
                | Some size -> Ok (Some (FsniDirective.Heap size))
                | None -> Error $"#heap requires an integer size"

            // Skip F# standard directives (handled by FCS)
            | "r" | "reference" | "I" | "nowarn" | "time" | "help" | "quit" ->
                Ok None

            | _ ->
                Error $"Unknown directive: #{directive}"
        else
            Error $"Invalid directive syntax: {trimmed}"
    else
        Ok None

/// Parse all directives from script source
let parseDirectives (source: string) : ScriptParseResult =
    let lines = source.Split([| '\n' |])
    let mutable directives = []
    let mutable directiveLines = []
    let mutable errors = []
    let mutable sourceLines = []

    for i, line in lines |> Array.indexed do
        let lineNum = i + 1
        match parseDirectiveLine line with
        | Ok (Some directive) ->
            directives <- directive :: directives
            directiveLines <- lineNum :: directiveLines
            // Replace directive with empty line to preserve line numbers
            sourceLines <- "" :: sourceLines
        | Ok None ->
            sourceLines <- line :: sourceLines
        | Error msg ->
            errors <- (lineNum, msg) :: errors
            sourceLines <- line :: sourceLines

    {
        Directives = List.rev directives
        DirectiveLines = List.rev directiveLines
        SourceWithoutDirectives = String.Join("\n", List.rev sourceLines)
        Errors = List.rev errors
    }

// =============================================================================
// Script Options
// =============================================================================

/// Options extracted from a .fsnx script
type FsnxScriptOptions = {
    /// Path to the script file
    ScriptPath: string
    /// Directory containing the script
    ScriptDirectory: string
    /// Target triple (from #target)
    Target: string
    /// Memory model (from #memory_model)
    MemoryModel: MemoryModelDirective
    /// Arena size (from #arena)
    ArenaSize: int option
    /// Max stack size (from #max_stack)
    MaxStackSize: int option
    /// Heap size (from #heap)
    HeapSize: int option
    /// Platform template (from #platform)
    Platform: string option
    /// Required dependencies (from #require)
    Dependencies: string list
    /// Included files (from #include)
    IncludedFiles: string list
    /// Loaded scripts (from #load)
    LoadedScripts: string list
    /// Source code without directives
    Source: string
    /// Parse errors
    ParseErrors: (int * string) list
}

let private defaultScriptOptions scriptPath = {
    ScriptPath = scriptPath
    ScriptDirectory = Path.GetDirectoryName(scriptPath)
    Target = "native"
    MemoryModel = MemoryModelDirective.Standard
    ArenaSize = None
    MaxStackSize = None
    HeapSize = None
    Platform = None
    Dependencies = []
    IncludedFiles = []
    LoadedScripts = []
    Source = ""
    ParseErrors = []
}

/// Parse a .fsnx script file and extract options
let parseScript (scriptPath: string) (source: string) : FsnxScriptOptions =
    let result = parseDirectives source
    let scriptDir = Path.GetDirectoryName(Path.GetFullPath(scriptPath))

    let resolvePath (p: string) =
        if Path.IsPathRooted(p) then p
        else Path.GetFullPath(Path.Combine(scriptDir, p))

    let mutable options = { defaultScriptOptions scriptPath with
                              ScriptDirectory = scriptDir
                              Source = result.SourceWithoutDirectives
                              ParseErrors = result.Errors }

    for directive in result.Directives do
        match directive with
        | FsniDirective.Target triple ->
            options <- { options with Target = triple }
        | FsniDirective.MemoryModel model ->
            options <- { options with MemoryModel = model }
        | FsniDirective.Arena size ->
            options <- { options with ArenaSize = Some size }
        | FsniDirective.MaxStack size ->
            options <- { options with MaxStackSize = Some size }
        | FsniDirective.Heap size ->
            options <- { options with HeapSize = Some size }
        | FsniDirective.Platform template ->
            options <- { options with Platform = Some template }
        | FsniDirective.Require dep ->
            options <- { options with Dependencies = dep :: options.Dependencies }
        | FsniDirective.Include path ->
            options <- { options with IncludedFiles = (resolvePath path) :: options.IncludedFiles }
        | FsniDirective.Load path ->
            options <- { options with LoadedScripts = (resolvePath path) :: options.LoadedScripts }

    { options with
        Dependencies = List.rev options.Dependencies
        IncludedFiles = List.rev options.IncludedFiles
        LoadedScripts = List.rev options.LoadedScripts }

/// Load and parse a .fsnx script from disk
let loadScript (scriptPath: string) : Result<FsnxScriptOptions, string> =
    try
        if not (File.Exists(scriptPath)) then
            Error $"Script file not found: {scriptPath}"
        else
            let source = File.ReadAllText(scriptPath)
            Ok (parseScript scriptPath source)
    with ex ->
        Error $"Error loading script {scriptPath}: {ex.Message}"

/// Check if a file is a native script
let isNativeScript (path: string) : bool =
    path.EndsWith(".fsnx", StringComparison.OrdinalIgnoreCase)

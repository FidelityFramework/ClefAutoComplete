/// Loader for .fidproj files (Fidelity project format)
/// Parses TOML configuration and produces FidprojOptions for FNCS integration.
/// See: https://speakez.tech/blog/native-fsharp-source-based-package-mgmt/
module FsNativeAutoComplete.Core.FidprojLoader

open System
open System.IO
open FsNativeAutoComplete.Core.TomlParser

// =============================================================================
// Fidproj Options Types
// =============================================================================

/// Memory model for native compilation
[<RequireQualifiedAccess>]
type MemoryModel =
    /// Stack-only allocation (no heap)
    | StackOnly
    /// Static pools for embedded systems
    | StaticPools
    /// Arena-based allocation
    | Arena
    /// Standard (allows heap allocation)
    | Standard

/// Output kind for the compiled binary
[<RequireQualifiedAccess>]
type OutputKind =
    /// Freestanding binary (no libc dependency)
    | Freestanding
    /// Console application (minimal libc)
    | Console
    /// Library
    | Library
    /// Embedded firmware
    | Embedded

/// A dependency reference in a .fidproj file
type FidprojDependency = {
    /// Name of the dependency
    Name: string
    /// Version constraint (e.g., "^0.5.0", "~0.3.2", "1.0.0")
    Version: string option
    /// Path to local source (for path dependencies)
    Path: string option
    /// Features to enable
    Features: string list
    /// Whether the dependency is optional
    Optional: bool
}

/// Optimization settings
type OptimizationSettings = {
    /// Optimization level (0-3)
    Level: int
    /// Inline threshold
    InlineThreshold: int option
    /// Eliminate closures
    EliminateClosures: bool
    /// Link-time optimization
    Lto: bool
}

/// Platform-specific settings
type PlatformSettings = {
    /// Platform template (e.g., "stm32l5")
    Template: string option
    /// Specific variant
    Variant: string option
    /// System clock frequency
    SystemClock: int64 option
    /// Flash size in KB
    FlashSize: int option
    /// RAM size in KB
    RamSize: int option
}

/// Build output format settings
type OutputFormats = {
    /// Generate Intel HEX
    Hex: bool
    /// Generate raw binary
    Bin: bool
    /// Generate ELF with debug symbols
    Elf: bool
}

/// Parsed .fidproj file options
type FidprojOptions = {
    /// Full path to the .fidproj file
    ProjectPath: string
    /// Project directory
    ProjectDirectory: string

    // Package metadata
    /// Package name
    Name: string
    /// Package version
    Version: string
    /// Description
    Description: string option
    /// Authors list
    Authors: string list
    /// License
    License: string option
    /// Repository URL
    Repository: string option
    /// Keywords
    Keywords: string list

    // Compilation settings
    /// Memory model
    MemoryModel: MemoryModel
    /// Target triple (e.g., "native", "thumbv8m.main-none-eabihf")
    Target: string
    /// Maximum stack size
    MaxStackSize: int option
    /// Heap size
    HeapSize: int option

    // Build settings
    /// Source files in compilation order
    SourceFiles: string list
    /// Resolved absolute paths to source files
    ResolvedSourceFiles: string list
    /// Output binary name
    OutputName: string option
    /// Output kind
    OutputKind: OutputKind
    /// Linker script path
    LinkerScript: string option

    // Dependencies
    /// Project dependencies
    Dependencies: FidprojDependency list
    /// Resolved Alloy library path
    AlloyPath: string option

    // Optional settings
    /// Optimization settings
    Optimization: OptimizationSettings
    /// Platform settings
    Platform: PlatformSettings
    /// Output format settings
    OutputFormats: OutputFormats
    /// Feature flags
    Features: Map<string, string list>
}

// =============================================================================
// Default Values
// =============================================================================

let private defaultOptimization = {
    Level = 0
    InlineThreshold = None
    EliminateClosures = false
    Lto = false
}

let private defaultPlatform = {
    Template = None
    Variant = None
    SystemClock = None
    FlashSize = None
    RamSize = None
}

let private defaultOutputFormats = {
    Hex = false
    Bin = false
    Elf = true
}

// =============================================================================
// Parsing Helpers
// =============================================================================

let private parseMemoryModel (s: string) : MemoryModel =
    match s.ToLowerInvariant() with
    | "stack_only" | "stackonly" -> MemoryModel.StackOnly
    | "static_pools" | "staticpools" -> MemoryModel.StaticPools
    | "arena" -> MemoryModel.Arena
    | "standard" | _ -> MemoryModel.Standard

let private parseOutputKind (s: string) : OutputKind =
    match s.ToLowerInvariant() with
    | "freestanding" -> OutputKind.Freestanding
    | "console" -> OutputKind.Console
    | "library" | "lib" -> OutputKind.Library
    | "embedded" -> OutputKind.Embedded
    | _ -> OutputKind.Console

let private parseDependency (name: string) (value: TomlValue) : FidprojDependency =
    match value with
    | TomlString version ->
        // Simple version string: alloy = "0.5.0"
        { Name = name
          Version = Some version
          Path = None
          Features = []
          Optional = false }
    | TomlInlineTable table ->
        // Inline table: alloy = { path = "/path", features = ["x"] }
        let version =
            match Map.tryFind "version" table with
            | Some (TomlString v) -> Some v
            | _ -> None
        let path =
            match Map.tryFind "path" table with
            | Some (TomlString p) -> Some p
            | _ -> None
        let features =
            match Map.tryFind "features" table with
            | Some (TomlArray arr) ->
                arr |> List.choose (function TomlString s -> Some s | _ -> None)
            | _ -> []
        let optional =
            match Map.tryFind "optional" table with
            | Some (TomlBool b) -> b
            | _ -> false
        { Name = name
          Version = version
          Path = path
          Features = features
          Optional = optional }
    | _ ->
        { Name = name
          Version = None
          Path = None
          Features = []
          Optional = false }

let private resolvePath (projectDir: string) (relativePath: string) : string =
    if Path.IsPathRooted(relativePath) then
        relativePath
    else
        Path.GetFullPath(Path.Combine(projectDir, relativePath))

// =============================================================================
// Main Loader
// =============================================================================

/// Load and parse a .fidproj file
let load (fidprojPath: string) : Result<FidprojOptions, string> =
    try
        if not (File.Exists(fidprojPath)) then
            Error $"Project file not found: {fidprojPath}"
        else
            let content = File.ReadAllText(fidprojPath)
            match parse content with
            | Error e -> Error $"Failed to parse {fidprojPath}: {e}"
            | Ok doc ->
                let projectDir = Path.GetDirectoryName(Path.GetFullPath(fidprojPath))

                // Package metadata
                let name = getString "package.name" doc |> Option.defaultValue (Path.GetFileNameWithoutExtension(fidprojPath))
                let version = getString "package.version" doc |> Option.defaultValue "0.0.0"
                let description = getString "package.description" doc
                let authors = getStringList "package.authors" doc |> Option.defaultValue []
                let license = getString "package.license" doc
                let repository = getString "package.repository" doc
                let keywords = getStringList "package.keywords" doc |> Option.defaultValue []

                // Compilation settings
                let memoryModel =
                    getString "compilation.memory_model" doc
                    |> Option.map parseMemoryModel
                    |> Option.defaultValue MemoryModel.Standard
                let target = getString "compilation.target" doc |> Option.defaultValue "native"
                let maxStackSize = getInt "compilation.max_stack_size" doc |> Option.map int
                let heapSize = getInt "compilation.heap_size" doc |> Option.map int

                // Build settings
                let sourceFiles = getStringList "build.sources" doc |> Option.defaultValue []
                let resolvedSourceFiles =
                    sourceFiles
                    |> List.map (resolvePath projectDir)
                let outputName = getString "build.output" doc
                let outputKind =
                    getString "build.output_kind" doc
                    |> Option.map parseOutputKind
                    |> Option.defaultValue OutputKind.Console
                let linkerScript =
                    getString "build.linker_script" doc
                    |> Option.map (resolvePath projectDir)

                // Dependencies
                let dependencies =
                    doc
                    |> Map.toList
                    |> List.choose (fun (key, value) ->
                        if key.StartsWith("dependencies.") then
                            let depName = key.Substring("dependencies.".Length)
                            // Skip sub-keys like dependencies.neural_net.version
                            if not (depName.Contains(".")) then
                                Some (parseDependency depName value)
                            else
                                None
                        else
                            None)

                let alloyPath =
                    dependencies
                    |> List.tryFind (fun d -> d.Name = "alloy")
                    |> Option.bind (fun d -> d.Path)
                    |> Option.map (resolvePath projectDir)

                // Optimization settings
                let optimization = {
                    Level = getInt "optimization.level" doc |> Option.map int |> Option.defaultValue 0
                    InlineThreshold = getInt "optimization.inline_threshold" doc |> Option.map int
                    EliminateClosures = getBool "optimization.eliminate_closures" doc |> Option.defaultValue false
                    Lto = getBool "optimization.lto" doc |> Option.defaultValue false
                }

                // Platform settings
                let platform = {
                    Template = getString "platform.template" doc
                    Variant = getString "platform.variant" doc
                    SystemClock = getInt "platform.system_clock" doc
                    FlashSize = getInt "platform.flash_size" doc |> Option.map int
                    RamSize = getInt "platform.ram_size" doc |> Option.map int
                }

                // Output formats
                let outputFormats = {
                    Hex = getBool "build.output_formats.hex" doc |> Option.defaultValue false
                    Bin = getBool "build.output_formats.bin" doc |> Option.defaultValue false
                    Elf = getBool "build.output_formats.elf" doc |> Option.defaultValue true
                }

                // Features
                let features =
                    doc
                    |> Map.toList
                    |> List.choose (fun (key, value) ->
                        if key.StartsWith("features.") then
                            let featureName = key.Substring("features.".Length)
                            match value with
                            | TomlArray arr ->
                                let deps = arr |> List.choose (function TomlString s -> Some s | _ -> None)
                                Some (featureName, deps)
                            | _ -> None
                        else
                            None)
                    |> Map.ofList

                Ok {
                    ProjectPath = Path.GetFullPath(fidprojPath)
                    ProjectDirectory = projectDir
                    Name = name
                    Version = version
                    Description = description
                    Authors = authors
                    License = license
                    Repository = repository
                    Keywords = keywords
                    MemoryModel = memoryModel
                    Target = target
                    MaxStackSize = maxStackSize
                    HeapSize = heapSize
                    SourceFiles = sourceFiles
                    ResolvedSourceFiles = resolvedSourceFiles
                    OutputName = outputName
                    OutputKind = outputKind
                    LinkerScript = linkerScript
                    Dependencies = dependencies
                    AlloyPath = alloyPath
                    Optimization = optimization
                    Platform = platform
                    OutputFormats = outputFormats
                    Features = features
                }
    with ex ->
        Error $"Error loading {fidprojPath}: {ex.Message}"

/// Try to find a .fidproj file in a directory
let tryFindInDirectory (directory: string) : string option =
    try
        Directory.GetFiles(directory, "*.fidproj")
        |> Array.tryHead
    with _ ->
        None

/// Get all source files for a project, including Alloy sources if available
let getProjectSources (options: FidprojOptions) : string list =
    let alloySources =
        match options.AlloyPath with
        | Some path when Directory.Exists(path) ->
            // Get Alloy source files in a reasonable order
            // Core files first, then others
            let coreFiles = ["Core.fs"; "Math.fs"; "Memory.fs"; "Text.fs"; "Platform.fs"; "Console.fs"]
            let allFiles = Directory.GetFiles(path, "*.fs") |> Array.toList

            let orderedCore =
                coreFiles
                |> List.choose (fun name ->
                    allFiles |> List.tryFind (fun f -> Path.GetFileName(f) = name))

            let remaining =
                allFiles
                |> List.filter (fun f ->
                    not (coreFiles |> List.contains (Path.GetFileName(f))))

            orderedCore @ remaining
        | _ -> []

    alloySources @ options.ResolvedSourceFiles

// =============================================================================
// Workspace Helpers (Native-First)
// =============================================================================

/// Native project file extensions
let projectExtensions = [| ".fidproj" |]

/// Native source file extensions
let sourceExtensions = [| ".fs"; ".fsi"; ".fsnx" |]

/// Find all .fidproj files in a directory (recursively)
let findAllProjects (rootDirectory: string) : string list =
    try
        Directory.GetFiles(rootDirectory, "*.fidproj", SearchOption.AllDirectories)
        |> Array.toList
    with _ ->
        []

/// Check if a source file belongs to a loaded project
let containsSourceFile (sourceFile: string) (options: FidprojOptions) : bool =
    let sourceFile = Path.GetFullPath(sourceFile)
    let sourceDir = Path.GetDirectoryName(sourceFile)

    // Check if source file is in the project's source list
    options.ResolvedSourceFiles
    |> List.exists (fun f -> String.Equals(f, sourceFile, StringComparison.OrdinalIgnoreCase))
    ||
    // Or if it's in the project directory
    String.Equals(options.ProjectDirectory, sourceDir, StringComparison.OrdinalIgnoreCase)

/// Find which project contains a source file
let findProjectForSourceFile (sourceFile: string) (projects: FidprojOptions list) : FidprojOptions option =
    projects |> List.tryFind (containsSourceFile sourceFile)

/// Check if a file is relevant for native workspace (project or source)
let isNativeWorkspaceFile (path: string) : bool =
    let ext = Path.GetExtension(path).ToLowerInvariant()
    projectExtensions |> Array.contains ext ||
    sourceExtensions |> Array.contains ext

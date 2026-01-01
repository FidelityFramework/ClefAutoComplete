namespace FsNativeAutoComplete.Core

/// Discriminates between native F# projects (.fidproj) and standard F# projects (.fsproj)
/// Native projects use FNCS (F# Native Compiler Services) for type checking
/// Standard projects use FCS (F# Compiler Services)
[<RequireQualifiedAccess>]
type ProjectKind =
    /// Native F# project (.fidproj) - uses FNCS, targets native compilation
    | Native
    /// Standard F# project (.fsproj) - uses FCS, targets .NET CLR
    | Standard

module ProjectKind =

    /// File extensions for native projects
    let nativeProjectExtensions = [| ".fidproj" |]

    /// File extensions for native script files
    let nativeScriptExtensions = [| ".fsnx" |]

    /// File extensions for standard F# projects
    let standardProjectExtensions = [| ".fsproj" |]

    /// File extensions for standard F# script files
    let standardScriptExtensions = [| ".fsx"; ".fsscript" |]

    /// Detect project kind from file path
    let detectFromPath (path: string) : ProjectKind =
        let lowerPath = path.ToLowerInvariant()
        if nativeProjectExtensions |> Array.exists lowerPath.EndsWith then
            ProjectKind.Native
        elif nativeScriptExtensions |> Array.exists lowerPath.EndsWith then
            ProjectKind.Native
        else
            ProjectKind.Standard

    /// Check if a file is a native project file
    let isNativeProject (path: string) : bool =
        detectFromPath path = ProjectKind.Native

    /// Check if a file is a native script file
    let isNativeScript (path: string) : bool =
        let lowerPath = path.ToLowerInvariant()
        nativeScriptExtensions |> Array.exists lowerPath.EndsWith

    /// Check if a file is a standard F# project file
    let isStandardProject (path: string) : bool =
        let lowerPath = path.ToLowerInvariant()
        standardProjectExtensions |> Array.exists lowerPath.EndsWith

    /// Check if a file is a standard F# script file
    let isStandardScript (path: string) : bool =
        let lowerPath = path.ToLowerInvariant()
        standardScriptExtensions |> Array.exists lowerPath.EndsWith

    /// Check if a source file belongs to a native project context
    /// This is used when we have a source file and need to determine
    /// which compiler service to use
    let isNativeSourceFile (path: string) : bool =
        let lowerPath = path.ToLowerInvariant()
        // .fsnx scripts are native
        if nativeScriptExtensions |> Array.exists lowerPath.EndsWith then true
        // Regular .fs files need context from their project
        else false

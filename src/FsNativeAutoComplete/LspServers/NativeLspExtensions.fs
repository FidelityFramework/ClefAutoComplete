/// Native LSP Extensions
/// Custom LSP endpoints for native F# compilation features.
/// These extend the standard LSP with Fidelity-specific functionality.
namespace FsNativeAutoComplete.Lsp

open System
open Ionide.LanguageServerProtocol.Types

// =============================================================================
// Custom Request/Response Types
// =============================================================================

/// Field layout information for a type
type FieldLayoutInfo = {
    /// Field name
    Name: string
    /// Field type
    FieldType: string
    /// Offset in bytes from start of type
    Offset: int
    /// Size in bytes
    Size: int
    /// Alignment requirement
    Alignment: int
}

/// Memory layout information for a type
type MemoryLayoutInfo = {
    /// Type name
    TypeName: string
    /// Total size in bytes
    Size: int
    /// Alignment requirement
    Alignment: int
    /// Field layouts (for records/structs)
    Fields: FieldLayoutInfo list
    /// Memory region (stack, heap, static, peripheral)
    Region: string
    /// Additional notes about the type layout
    Notes: string option
}

/// Request parameters for memory layout
type MemoryLayoutParams = {
    /// Text document
    TextDocument: TextDocumentIdentifier
    /// Position in the document
    Position: Position
}

/// SRTP witness resolution information
type SrtpWitnessInfo = {
    /// The SRTP constraint (e.g., "op_Addition")
    Operator: string
    /// The argument type(s) involved
    ArgumentTypes: string list
    /// The resolved member name
    ResolvedMember: string
    /// The type providing the implementation
    ImplementationType: string
    /// Implementation details
    Implementation: string option
}

/// Request parameters for SRTP witnesses
type SrtpWitnessParams = {
    /// Text document
    TextDocument: TextDocumentIdentifier
    /// Position in the document
    Position: Position
}

/// Platform binding information
type PlatformBindingInfo = {
    /// Binding name (e.g., "writeBytes")
    Name: string
    /// Signature
    Signature: string
    /// Description
    Description: string option
    /// Platform-specific notes (syscall numbers, etc.)
    PlatformNotes: string option
}

/// Request parameters for platform bindings
type PlatformBindingsParams = {
    /// Text document
    TextDocument: TextDocumentIdentifier
    /// Position in the document (optional - if not provided, lists all bindings in scope)
    Position: Position option
}

/// Script options information
type ScriptOptionsInfo = {
    /// Target triple
    Target: string
    /// Memory model
    MemoryModel: string
    /// Arena size (if applicable)
    ArenaSize: int option
    /// Max stack size (if applicable)
    MaxStackSize: int option
    /// Heap size (if applicable)
    HeapSize: int option
    /// Platform template (if applicable)
    Platform: string option
    /// Dependencies
    Dependencies: string list
    /// Included files
    IncludedFiles: string list
    /// Parse errors
    ParseErrors: (int * string) list
}

/// Request parameters for script options
type ScriptOptionsParams = {
    /// Text document (must be a .fsnx file)
    TextDocument: TextDocumentIdentifier
}

// =============================================================================
// Custom LSP Extension Methods
// =============================================================================

/// Module containing native LSP extension method names
[<RequireQualifiedAccess>]
module NativeLspMethods =
    /// Get memory layout for type at position
    let memoryLayout = "fsnative/memoryLayout"

    /// Get SRTP witness information at position
    let srtpWitnesses = "fsnative/srtpWitnesses"

    /// Get platform bindings information
    let platformBindings = "fsnative/platformBindings"

    /// Get script options (for .fsnx files)
    let scriptOptions = "fsnative/scriptOptions"

    /// Compile to native binary
    let compile = "fsnative/compile"

    /// Get native diagnostics summary
    let diagnosticsSummary = "fsnative/diagnosticsSummary"

// =============================================================================
// Extension Handlers
// =============================================================================

/// Handles native LSP extension requests
type NativeLspExtensionHandler(nativeState: NativeState) =

    /// Get memory layout for type at position
    member _.GetMemoryLayout(_params: MemoryLayoutParams) : MemoryLayoutInfo option =
        // TODO: Implement when FNCS provides type layout info
        // For now, return None
        None

    /// Get SRTP witness information at position
    member _.GetSrtpWitnesses(_params: SrtpWitnessParams) : SrtpWitnessInfo list =
        // TODO: Implement when FNCS provides SRTP resolution info
        // For now, return empty list
        []

    /// Get platform bindings information
    member _.GetPlatformBindings(_params: PlatformBindingsParams) : PlatformBindingInfo list =
        // Return known platform bindings
        // This is a static list for now; could be dynamically discovered later
        [
            { Name = "writeBytes"
              Signature = "fd: int -> buffer: nativeptr<byte> -> count: int -> int"
              Description = Some "Write bytes to a file descriptor"
              PlatformNotes = Some "Linux: syscall 1 (write)" }

            { Name = "readBytes"
              Signature = "fd: int -> buffer: nativeptr<byte> -> maxCount: int -> int"
              Description = Some "Read bytes from a file descriptor"
              PlatformNotes = Some "Linux: syscall 0 (read)" }

            { Name = "getCurrentTicks"
              Signature = "unit -> int64"
              Description = Some "Get current time in ticks"
              PlatformNotes = Some "Uses clock_gettime on Linux" }

            { Name = "sleep"
              Signature = "milliseconds: int -> unit"
              Description = Some "Sleep for specified milliseconds"
              PlatformNotes = Some "Linux: nanosleep syscall" }

            { Name = "exit"
              Signature = "code: int -> unit"
              Description = Some "Exit the process"
              PlatformNotes = Some "Linux: syscall 60 (exit)" }
        ]

    /// Get script options for a .fsnx file
    member _.GetScriptOptions(params': ScriptOptionsParams) : ScriptOptionsInfo option =
        let filePath = params'.TextDocument.Uri.Replace("file://", "")
        match nativeState.GetScript(filePath) with
        | Some script ->
            let opts = script.Options
            Some {
                Target = opts.Target
                MemoryModel =
                    match opts.MemoryModel with
                    | FsNativeAutoComplete.Core.FsniDirectives.MemoryModelDirective.StackOnly -> "stack_only"
                    | FsNativeAutoComplete.Core.FsniDirectives.MemoryModelDirective.StaticPools -> "static_pools"
                    | FsNativeAutoComplete.Core.FsniDirectives.MemoryModelDirective.Arena -> "arena"
                    | FsNativeAutoComplete.Core.FsniDirectives.MemoryModelDirective.Standard -> "standard"
                ArenaSize = opts.ArenaSize
                MaxStackSize = opts.MaxStackSize
                HeapSize = opts.HeapSize
                Platform = opts.Platform
                Dependencies = opts.Dependencies
                IncludedFiles = opts.IncludedFiles
                ParseErrors = opts.ParseErrors
            }
        | None -> None

/// Native Compiler Service Interface
/// Wraps FNCS (F# Native Compiler Services) for LSP integration.
/// This module is only available when HAVE_FNCS is defined (net9.0+).
///
/// Key architectural features (see "Standing Art" design):
/// - Quotations: Display as compile-time semantic carriers (not runtime evaluated)
/// - Active Patterns: Show compositional structural recognition
/// - Computation Expressions: Format continuation capture notation
/// - Native Types: Display with fsnative semantics (voption, platform words, fat pointers)
module FsNativeAutoComplete.Core.NativeCompilerServiceInterface

#if HAVE_FNCS

open System
open System.IO
open FSharp.Native.Compiler.Checking.Native.NativeTypes
open FSharp.Native.Compiler.Checking.Native.SemanticGraph
open FSharp.Native.Compiler.NativeService

// =============================================================================
// Position Types (for LSP integration)
// =============================================================================

/// A position in a document (0-indexed line and column)
[<Struct>]
type DocumentPosition = {
    Line: int
    Column: int
}

/// A range in a document
[<Struct>]
type DocumentRange = {
    Start: DocumentPosition
    End: DocumentPosition
}

// =============================================================================
// LSP-Compatible Types
// =============================================================================

/// Severity levels matching LSP DiagnosticSeverity
[<RequireQualifiedAccess>]
type LspDiagnosticSeverity =
    | Error = 1
    | Warning = 2
    | Information = 3
    | Hint = 4

/// A diagnostic compatible with LSP
type NativeDiagnostic = {
    /// Severity level
    Severity: LspDiagnosticSeverity
    /// Error code (e.g., "FS8100")
    Code: string
    /// Diagnostic message
    Message: string
    /// Location in source
    Range: DocumentRange
    /// Source identifier
    Source: string
}

/// Hover information for a position
type NativeHoverInfo = {
    /// Markdown content for the hover
    Contents: string
    /// Range that the hover applies to
    Range: DocumentRange option
}

/// A completion item
type NativeCompletionItem = {
    /// Display label
    Label: string
    /// Detailed information
    Detail: string option
    /// Documentation
    Documentation: string option
    /// Kind of item (1=Function, 6=Variable, 7=Class, 22=Struct, etc.)
    Kind: int
    /// Text to insert
    InsertText: string option
}

// =============================================================================
// Native Check Result Wrapper
// =============================================================================

/// Result of parsing and checking a native file
type NativeCheckResult = {
    /// The semantic graph (if successful)
    Graph: SemanticGraph option
    /// All diagnostics (errors and warnings)
    Diagnostics: NativeDiagnostic list
    /// Whether there were any errors
    HasErrors: bool
    /// File path
    FilePath: string
}

// =============================================================================
// Conversion Functions
// =============================================================================

let private toDocumentPosition (pos: Position) : DocumentPosition =
    { Line = pos.Line - 1; Column = pos.Column }  // FCS uses 1-indexed lines

let private toDocumentRange (range: SourceRange) : DocumentRange =
    { Start = toDocumentPosition range.Start
      End = toDocumentPosition range.End }

let private toLspSeverity (severity: NativeDiagnosticSeverity) : LspDiagnosticSeverity =
    match severity with
    | NativeDiagnosticSeverity.Error -> LspDiagnosticSeverity.Error
    | NativeDiagnosticSeverity.Warning -> LspDiagnosticSeverity.Warning
    | NativeDiagnosticSeverity.Info -> LspDiagnosticSeverity.Information

let private toNativeDiagnostic (diag: Diagnostic) : NativeDiagnostic =
    { Severity = toLspSeverity diag.Severity
      Code = diag.Code
      Message = diag.Message
      Range = toDocumentRange diag.Range
      Source = "fsnative" }

// =============================================================================
// Type Formatting for Native Types
// =============================================================================

/// Format a native type for hover display with native semantics
let formatNativeType (ty: NativeType) : string =
    // Use FNCS's formatType but we can enhance it for LSP display
    formatType ty

/// Format a type with native-specific documentation
let formatNativeTypeWithDocs (ty: NativeType) : string =
    let baseFormat = formatType ty

    // Add native-specific notes for common types
    match ty with
    | NativeType.TApp(tc, _) when tc.Name = "string" ->
        $"{baseFormat}  \n*UTF-8 fat pointer {{ ptr: *u8, len: usize }}*"
    | NativeType.TApp(tc, [_]) when tc.Name = "option" ->
        $"{baseFormat}  \n*voption (value-type, stack-allocated)*"
    | NativeType.TApp(tc, _) when tc.Name = "int" ->
        $"{baseFormat}  \n*Platform word (isize)*"
    | NativeType.TApp(tc, _) when tc.Name = "uint" ->
        $"{baseFormat}  \n*Platform word (usize)*"
    | NativeType.TByref(_, ByrefKind.In) ->
        $"{baseFormat}  \n*Read-only reference*"
    | NativeType.TByref(_, ByrefKind.Out) ->
        $"{baseFormat}  \n*Write-only reference*"
    | NativeType.TByref(_, ByrefKind.InOut) ->
        $"{baseFormat}  \n*Mutable reference*"
    | NativeType.TNativePtr _ ->
        $"{baseFormat}  \n*Native pointer (unsafe)*"
    | _ -> baseFormat

/// Format SRTP resolution for hover display
let formatSRTPResolution (witness: WitnessResolution) : string =
    let implStr =
        match witness.Implementation with
        | WitnessImplementation.Direct(path, name) ->
            $"Direct call to {formatModulePath path}.{name}"
        | WitnessImplementation.InstanceMethod name ->
            $"Instance method: {name}"
        | WitnessImplementation.StaticMethod(path, name) ->
            $"Static method: {formatModulePath path}.{name}"
        | WitnessImplementation.Builtin kind ->
            $"Built-in: {kind}"

    $"**SRTP Resolution**  \nOperator: `{witness.Operator}`  \nArg type: `{formatType witness.ArgType}`  \nResolved: `{witness.ResolvedMember}`  \n{implStr}"

// =============================================================================
// Position-Based Lookups
// =============================================================================

/// Check if a source range contains a position
let private rangeContains (range: SourceRange) (line: int) (col: int) : bool =
    let l = line + 1  // Convert to 1-indexed
    if l < range.Start.Line || l > range.End.Line then false
    elif l = range.Start.Line && col < range.Start.Column then false
    elif l = range.End.Line && col > range.End.Column then false
    else true

/// Find the innermost node at a given position
let findNodeAtPosition (graph: SemanticGraph) (filePath: string) (line: int) (col: int) : SemanticNode option =
    let fileName = Path.GetFileName(filePath)

    // Find all nodes that contain this position
    let containingNodes =
        graph.Nodes
        |> Map.values
        |> Seq.filter (fun node ->
            (node.Range.File = filePath || node.Range.File = fileName) &&
            rangeContains node.Range line col)
        |> Seq.toList

    // Return the innermost (smallest range) node
    containingNodes
    |> List.sortBy (fun node ->
        let range = node.Range
        let lines = range.End.Line - range.Start.Line
        let cols = if lines = 0 then range.End.Column - range.Start.Column else lines * 1000
        lines * 1000 + cols)
    |> List.tryHead

// =============================================================================
// Native Checker
// =============================================================================

/// The native checker wrapping FNCS
type FSharpNativeChecker() =

    /// Parse and check a single file
    member _.ParseAndCheckFile(filePath: string, source: string) : NativeCheckResult =
        match parseAndCheck source filePath with
        | Success result ->
            { Graph = Some result.Graph
              Diagnostics = result.Diagnostics |> List.map toNativeDiagnostic
              HasErrors = CheckResult.hasErrors result
              FilePath = filePath }
        | ParseFailure errors ->
            // ParseFailure returns string list, convert to diagnostics
            let diagnostics =
                errors
                |> List.map (fun msg ->
                    { Severity = LspDiagnosticSeverity.Error
                      Code = "FS0001"
                      Message = msg
                      Range = { Start = { Line = 0; Column = 0 }
                                End = { Line = 0; Column = 0 } }
                      Source = "fsnative" })
            { Graph = None
              Diagnostics = diagnostics
              HasErrors = true
              FilePath = filePath }
        | CheckFailure result ->
            { Graph = Some result.Graph
              Diagnostics = result.Diagnostics |> List.map toNativeDiagnostic
              HasErrors = true
              FilePath = filePath }

    /// Parse and check multiple files in order
    member this.ParseAndCheckFiles(files: (string * string) list) : NativeCheckResult list =
        files |> List.map (fun (path, source) -> this.ParseAndCheckFile(path, source))

    /// Get hover information at a position
    member _.GetHoverInfo(result: NativeCheckResult, line: int, col: int) : NativeHoverInfo option =
        match result.Graph with
        | None -> None
        | Some graph ->
            match findNodeAtPosition graph result.FilePath line col with
            | None -> None
            | Some node ->
                let typeInfo = formatNativeTypeWithDocs node.Type

                let kindInfo =
                    match node.Kind with
                    | SemanticKind.Binding(name, isMutable, isRec) ->
                        let mutStr = if isMutable then "mutable " else ""
                        let recStr = if isRec then "rec " else ""
                        $"**{mutStr}{recStr}let {name}**"
                    | SemanticKind.Lambda(params', _) ->
                        let paramStr = params' |> List.map (fun (n, t) -> $"{n}: {formatType t}") |> String.concat ", "
                        $"**lambda ({paramStr})**"
                    | SemanticKind.VarRef(name, _) ->
                        $"**{name}**"
                    | SemanticKind.Application(_, _) ->
                        "**function application**"
                    | SemanticKind.Literal value ->
                        match value with
                        | LiteralValue.String s -> $"**string literal**: \"{s}\""
                        | LiteralValue.Int32 i -> $"**int32 literal**: {i}"
                        | LiteralValue.Int64 i -> $"**int64 literal**: {i}L"
                        | LiteralValue.Float64 f -> $"**float literal**: {f}"
                        | LiteralValue.Bool b -> $"**bool literal**: {b}"
                        | _ -> "**literal**"
                    | SemanticKind.PlatformBinding name ->
                        $"**Platform.Binding**: `{name}`  \n*Provided by Alex at compile time*"
                    | SemanticKind.TraitCall(memberName, _, _) ->
                        $"**SRTP trait call**: `{memberName}`"
                    | _ -> ""

                let srtpInfo =
                    match node.SRTPResolution with
                    | Some witness -> "\n\n---\n" + formatSRTPResolution witness
                    | None -> ""

                let content =
                    [ kindInfo
                      $"```fsnative\n{typeInfo}\n```"
                      srtpInfo ]
                    |> List.filter (not << String.IsNullOrEmpty)
                    |> String.concat "\n\n"

                Some {
                    Contents = content
                    Range = Some (toDocumentRange node.Range)
                }

    /// Get completions at a position
    member _.GetCompletions(result: NativeCheckResult, _line: int, _col: int) : NativeCompletionItem list =
        match result.Graph with
        | None -> []
        | Some graph ->
            // Get all bindings as completion items
            let bindings = SemanticGraph.bindings graph

            bindings
            |> List.choose (fun node ->
                match node.Kind with
                | SemanticKind.Binding(name, _, _) ->
                    Some {
                        Label = name
                        Detail = Some (formatType node.Type)
                        Documentation = None
                        Kind = 6  // Variable
                        InsertText = Some name
                    }
                | _ -> None)

    /// Get all diagnostics for a file
    member _.GetDiagnostics(result: NativeCheckResult) : NativeDiagnostic list =
        result.Diagnostics

#else

// Stub types when FNCS is not available (net8.0)

/// Position placeholder
[<Struct>]
type DocumentPosition = { Line: int; Column: int }

/// Range placeholder
[<Struct>]
type DocumentRange = { Start: DocumentPosition; End: DocumentPosition }

/// Severity placeholder
[<RequireQualifiedAccess>]
type LspDiagnosticSeverity = | Error = 1 | Warning = 2 | Information = 3 | Hint = 4

/// Diagnostic placeholder
type NativeDiagnostic = {
    Severity: LspDiagnosticSeverity
    Code: string
    Message: string
    Range: DocumentRange
    Source: string
}

/// Hover placeholder
type NativeHoverInfo = { Contents: string; Range: DocumentRange option }

/// Completion placeholder
type NativeCompletionItem = {
    Label: string
    Detail: string option
    Documentation: string option
    Kind: int
    InsertText: string option
}

/// Result placeholder
type NativeCheckResult = {
    Graph: unit option
    Diagnostics: NativeDiagnostic list
    HasErrors: bool
    FilePath: string
}

/// Stub checker that does nothing on net8.0
type FSharpNativeChecker() =
    member _.ParseAndCheckFile(_: string, _: string) : NativeCheckResult =
        { Graph = None
          Diagnostics = [{ Severity = LspDiagnosticSeverity.Error
                           Code = "FS0000"
                           Message = "FNCS not available on .NET 8. Use .NET 9+ for native project support."
                           Range = { Start = { Line = 0; Column = 0 }; End = { Line = 0; Column = 0 } }
                           Source = "fsnative" }]
          HasErrors = true
          FilePath = "" }

    member this.ParseAndCheckFiles(files: (string * string) list) : NativeCheckResult list =
        files |> List.map (fun (path, source) -> this.ParseAndCheckFile(path, source))

    member _.GetHoverInfo(_: NativeCheckResult, _: int, _: int) : NativeHoverInfo option = None

    member _.GetCompletions(_: NativeCheckResult, _: int, _: int) : NativeCompletionItem list = []

    member _.GetDiagnostics(result: NativeCheckResult) : NativeDiagnostic list = result.Diagnostics

#endif

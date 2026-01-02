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
open FsNativeAutoComplete.Logging

let private logger = LogProvider.getLoggerByName "FsNative"

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

/// Definition location result
type NativeDefinitionResult = {
    /// File path of the definition
    FilePath: string
    /// Range of the definition
    Range: DocumentRange
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
    let logger = LogProvider.getLoggerByName "FsNative"

    // First, find all nodes that match the file
    let nodesForFile =
        graph.Nodes
        |> Map.values
        |> Seq.filter (fun node ->
            node.Range.File = filePath || node.Range.File = fileName)
        |> Seq.toList

    logger.debug (
        Log.setMessage "findNodeAtPosition: {count} nodes for file {file} (full path: {fullPath})"
        >> Log.addContextDestructured "count" nodesForFile.Length
        >> Log.addContextDestructured "file" fileName
        >> Log.addContextDestructured "fullPath" filePath
    )

    // Log a sample of node kinds and ranges for this file
    if nodesForFile.Length > 0 && nodesForFile.Length <= 20 then
        for node in nodesForFile do
            logger.debug (
                Log.setMessage "  Node {id}: {kind} at {startLine}:{startCol}-{endLine}:{endCol} in {file}"
                >> Log.addContextDestructured "id" (NodeId.value node.Id)
                >> Log.addContextDestructured "kind" (sprintf "%A" node.Kind |> fun s -> if s.Length > 50 then s.Substring(0, 50) + "..." else s)
                >> Log.addContextDestructured "startLine" node.Range.Start.Line
                >> Log.addContextDestructured "startCol" node.Range.Start.Column
                >> Log.addContextDestructured "endLine" node.Range.End.Line
                >> Log.addContextDestructured "endCol" node.Range.End.Column
                >> Log.addContextDestructured "file" node.Range.File
            )
    elif nodesForFile.Length > 20 then
        logger.debug (
            Log.setMessage "  (Too many nodes to list, showing first 10)"
        )
        for node in nodesForFile |> List.take 10 do
            logger.debug (
                Log.setMessage "  Node {id}: {kind} at {startLine}:{startCol}-{endLine}:{endCol}"
                >> Log.addContextDestructured "id" (NodeId.value node.Id)
                >> Log.addContextDestructured "kind" (sprintf "%A" node.Kind |> fun s -> if s.Length > 50 then s.Substring(0, 50) + "..." else s)
                >> Log.addContextDestructured "startLine" node.Range.Start.Line
                >> Log.addContextDestructured "startCol" node.Range.Start.Column
                >> Log.addContextDestructured "endLine" node.Range.End.Line
                >> Log.addContextDestructured "endCol" node.Range.End.Column
            )

    // Log unique file paths in the graph for debugging
    let uniqueFiles =
        graph.Nodes
        |> Map.values
        |> Seq.map (fun n -> n.Range.File)
        |> Seq.distinct
        |> Seq.toList

    logger.debug (
        Log.setMessage "findNodeAtPosition: Unique files in graph: {files}"
        >> Log.addContextDestructured "files" uniqueFiles
    )

    // Find all nodes that contain this position
    let containingNodes =
        nodesForFile
        |> List.filter (fun node -> rangeContains node.Range line col)

    logger.debug (
        Log.setMessage "findNodeAtPosition: {count} nodes contain position line={line} col={col}"
        >> Log.addContextDestructured "count" containingNodes.Length
        >> Log.addContextDestructured "line" line
        >> Log.addContextDestructured "col" col
    )

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

    /// Parse and check multiple files in order (ISOLATED - each file is checked separately)
    /// DEPRECATED: Use ParseAndCheckProject for proper multi-file type resolution
    member this.ParseAndCheckFiles(files: (string * string) list) : NativeCheckResult list =
        files |> List.map (fun (path, source) -> this.ParseAndCheckFile(path, source))

    /// Parse and check multiple files as a project with shared type environment.
    /// Files are processed in order: Alloy sources first, then project sources.
    /// This enables proper type resolution across file boundaries.
    member _.ParseAndCheckProject(files: (string * string) list) : NativeCheckResult =
        logger.info (
            Log.setMessage "ParseAndCheckProject: Checking {count} files"
            >> Log.addContextDestructured "count" (List.length files)
        )
        for (path, _) in files do
            logger.debug (
                Log.setMessage "ParseAndCheckProject: File {path}"
                >> Log.addContextDestructured "path" path
            )

        if List.isEmpty files then
            { Graph = None
              Diagnostics = []
              HasErrors = false
              FilePath = "" }
        else
            // Parse all files first
            let parseResults =
                files
                |> List.map (fun (path, source) ->
                    logger.debug (
                        Log.setMessage "Parsing {path} ({chars} chars)"
                        >> Log.addContextDestructured "path" path
                        >> Log.addContextDestructured "chars" source.Length
                    )
                    match parseStringWithDefaults source path with
                    | ParseSuccess input ->
                        logger.debug (
                            Log.setMessage "Parse SUCCESS: {path}"
                            >> Log.addContextDestructured "path" path
                        )
                        Some (path, input)
                    | ParseError errors ->
                        logger.warn (
                            Log.setMessage "Parse FAILED: {path} - {errors}"
                            >> Log.addContextDestructured "path" path
                            >> Log.addContextDestructured "errors" errors
                        )
                        None)
                |> List.choose id

            if List.isEmpty parseResults then
                // All files failed to parse
                { Graph = None
                  Diagnostics = [{ Severity = LspDiagnosticSeverity.Error
                                   Code = "FS0001"
                                   Message = "All files failed to parse"
                                   Range = { Start = { Line = 0; Column = 0 }
                                             End = { Line = 0; Column = 0 } }
                                   Source = "fsnative" }]
                  HasErrors = true
                  FilePath = files |> List.tryHead |> Option.map fst |> Option.defaultValue "" }
            else
                // Check all files together with shared environment
                logger.info (
                    Log.setMessage "Checking {count} parsed files together"
                    >> Log.addContextDestructured "count" (List.length parseResults)
                )
                let parsedInputs = parseResults |> List.map snd
                let result = checkParsedInputs parsedInputs

                logger.info (
                    Log.setMessage "Check result: {nodes} nodes, {diagnostics} diagnostics, hasErrors={hasErrors}"
                    >> Log.addContextDestructured "nodes" result.Graph.Nodes.Count
                    >> Log.addContextDestructured "diagnostics" (List.length result.Diagnostics)
                    >> Log.addContextDestructured "hasErrors" (CheckResult.hasErrors result)
                )

                let diagnostics = result.Diagnostics |> List.map toNativeDiagnostic

                { Graph = Some result.Graph
                  Diagnostics = diagnostics
                  HasErrors = CheckResult.hasErrors result
                  FilePath = files |> List.tryHead |> Option.map fst |> Option.defaultValue "" }

    /// Format information about a definition for hover display
    member private _.FormatDefinitionInfo(_graph: SemanticGraph, defNode: SemanticNode) : string =
        let fileName = Path.GetFileName(defNode.Range.File)
        let defKind =
            match defNode.Kind with
            | SemanticKind.Binding(name, isMutable, isRec) ->
                let mutStr = if isMutable then "mutable " else ""
                let recStr = if isRec then "rec " else ""
                $"**Defined as**: `{mutStr}{recStr}let {name}`"
            | SemanticKind.Lambda _ -> "**Defined as**: lambda"
            | SemanticKind.PlatformBinding name ->
                $"**Platform.Binding**: `{name}`  \n*Provided by Alex at compile time*"
            | _ -> ""

        let defType = formatNativeTypeWithDocs defNode.Type
        let defSrtp =
            match defNode.SRTPResolution with
            | Some witness -> "\n" + formatSRTPResolution witness
            | None -> ""

        $"\n\n---\n**Definition** ({fileName}:{defNode.Range.Start.Line})\n\n{defKind}\n\n```fsnative\n{defType}\n```{defSrtp}"

    /// Get hover information at a position, following references to definitions
    /// filePath: the actual file being queried (not result.FilePath which is the first file in multi-file check)
    member this.GetHoverInfo(result: NativeCheckResult, filePath: string, line: int, col: int) : NativeHoverInfo option =
        match result.Graph with
        | None ->
            logger.debug (
                Log.setMessage "Hover: No graph available for {path}"
                >> Log.addContextDestructured "path" filePath
            )
            None
        | Some graph ->
            logger.debug (
                Log.setMessage "Hover: Graph has {nodes} nodes, looking at line={line} col={col} in {path}"
                >> Log.addContextDestructured "nodes" graph.Nodes.Count
                >> Log.addContextDestructured "line" line
                >> Log.addContextDestructured "col" col
                >> Log.addContextDestructured "path" filePath
            )

            match findNodeAtPosition graph filePath line col with
            | None ->
                logger.debug (Log.setMessage "Hover: No node found at position")
                None
            | Some node ->
                logger.info (
                    Log.setMessage "Hover: Found node {id} of kind {kind}"
                    >> Log.addContextDestructured "id" node.Id
                    >> Log.addContextDestructured "kind" (sprintf "%A" node.Kind)
                )

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
                    | SemanticKind.VarRef(name, _defId) ->
                        // Show basic reference info (definition lookup done separately below)
                        $"**{name}** (reference)"
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

                // Follow VarRef to its definition and show definition info
                let definitionInfo =
                    match node.Kind with
                    | SemanticKind.VarRef(name, Some defId) ->
                        match SemanticGraph.tryGetNode defId graph with
                        | Some defNode ->
                            logger.info (
                                Log.setMessage "Hover: VarRef {name} resolves to definition at {file}:{line}"
                                >> Log.addContextDestructured "name" name
                                >> Log.addContextDestructured "file" defNode.Range.File
                                >> Log.addContextDestructured "line" defNode.Range.Start.Line
                            )
                            this.FormatDefinitionInfo(graph, defNode)
                        | None ->
                            logger.debug (
                                Log.setMessage "Hover: VarRef {name} has defId {defId} but node not found"
                                >> Log.addContextDestructured "name" name
                                >> Log.addContextDestructured "defId" (NodeId.value defId)
                            )
                            ""
                    | SemanticKind.VarRef(name, None) ->
                        logger.debug (
                            Log.setMessage "Hover: VarRef {name} has no definition link"
                            >> Log.addContextDestructured "name" name
                        )
                        ""
                    | _ -> ""

                let srtpInfo =
                    match node.SRTPResolution with
                    | Some witness -> "\n\n---\n" + formatSRTPResolution witness
                    | None -> ""

                let content =
                    [ kindInfo
                      $"```fsnative\n{typeInfo}\n```"
                      definitionInfo
                      srtpInfo ]
                    |> List.filter (not << String.IsNullOrEmpty)
                    |> String.concat "\n\n"

                logger.info (
                    Log.setMessage "Hover: Returning content of length {len}: {preview}"
                    >> Log.addContextDestructured "len" content.Length
                    >> Log.addContextDestructured "preview" (if content.Length > 100 then content.Substring(0, 100) + "..." else content)
                )

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

    /// Get definition location for symbol at a position
    member _.GetDefinition(result: NativeCheckResult, line: int, col: int) : NativeDefinitionResult option =
        match result.Graph with
        | None -> None
        | Some graph ->
            match findNodeAtPosition graph result.FilePath line col with
            | None -> None
            | Some node ->
                match node.Kind with
                | SemanticKind.VarRef(name, _) ->
                    // Find the binding definition for this variable reference
                    let bindings = SemanticGraph.bindings graph
                    bindings
                    |> List.tryFind (fun bindingNode ->
                        match bindingNode.Kind with
                        | SemanticKind.Binding(bindingName, _, _) -> bindingName = name
                        | _ -> false)
                    |> Option.map (fun defNode ->
                        { FilePath = defNode.Range.File
                          Range = toDocumentRange defNode.Range })
                | SemanticKind.Binding(_, _, _) ->
                    // Already at the definition
                    Some { FilePath = node.Range.File
                           Range = toDocumentRange node.Range }
                | _ -> None

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

/// Definition result placeholder
type NativeDefinitionResult = {
    FilePath: string
    Range: DocumentRange
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

    member this.ParseAndCheckProject(files: (string * string) list) : NativeCheckResult =
        // Stub: just return empty result on net8.0
        { Graph = None
          Diagnostics = [{ Severity = LspDiagnosticSeverity.Error
                           Code = "FS0000"
                           Message = "FNCS not available on .NET 8. Use .NET 9+ for native project support."
                           Range = { Start = { Line = 0; Column = 0 }; End = { Line = 0; Column = 0 } }
                           Source = "fsnative" }]
          HasErrors = true
          FilePath = files |> List.tryHead |> Option.map fst |> Option.defaultValue "" }

    member _.GetHoverInfo(_: NativeCheckResult, _: int, _: int) : NativeHoverInfo option = None

    member _.GetCompletions(_: NativeCheckResult, _: int, _: int) : NativeCompletionItem list = []

    member _.GetDiagnostics(result: NativeCheckResult) : NativeDiagnostic list = result.Diagnostics

    member _.GetDefinition(_: NativeCheckResult, _: int, _: int) : NativeDefinitionResult option = None

#endif

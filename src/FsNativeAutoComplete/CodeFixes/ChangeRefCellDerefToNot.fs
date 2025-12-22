module FsNativeAutoComplete.CodeFix.ChangeRefCellDerefToNot

open FsToolkit.ErrorHandling
open FsNativeAutoComplete.CodeFix.Types
open Ionide.LanguageServerProtocol.Types
open FsNativeAutoComplete
open FsNativeAutoComplete.LspHelpers

let title = "Use 'not' to negate expression"

/// a codefix that changes a ref cell deref (!) to a call to 'not'
let fix (getParseResultsForFile: GetParseResultsForFile) : CodeFix =
  Run.ifDiagnosticByCode (Set.ofList [ "1" ]) (fun diagnostic codeActionParams ->
    asyncResult {
      let fileName = codeActionParams.TextDocument.GetFilePath() |> Utils.normalizePath

      let fcsPos = protocolPosToPos diagnostic.Range.Start
      let! tyRes, _line, _lines = getParseResultsForFile fileName fcsPos

      match tyRes.GetParseResults.TryRangeOfRefCellDereferenceContainingPos fcsPos with
      | Some derefRange ->
        return
          [ { SourceDiagnostic = Some diagnostic
              Title = title
              File = codeActionParams.TextDocument
              Edits =
                [| { Range = fcsRangeToLsp derefRange
                     NewText = "not " } |]
              Kind = FixKind.Fix } ]
      | None -> return []
    })

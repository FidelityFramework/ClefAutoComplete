module FsNativeAutoComplete.CodeFix.ConvertDoubleEqualsToSingleEquals

open FsToolkit.ErrorHandling
open FsNativeAutoComplete.CodeFix.Types
open FsNativeAutoComplete
open FsNativeAutoComplete.LspHelpers

let title = "Use '=' for equality check"

/// a codefix that corrects == equality to = equality
let fix (getRangeText: GetRangeText) : CodeFix =
  Run.ifDiagnosticByCode (Set.ofList [ "43" ]) (fun diagnostic codeActionParams ->
    asyncResult {
      let fileName = codeActionParams.TextDocument.GetFilePath() |> Utils.normalizePath

      let! errorText = getRangeText fileName diagnostic.Range

      match errorText with
      | "==" ->
        return
          [ { Title = title
              File = codeActionParams.TextDocument
              SourceDiagnostic = Some diagnostic
              Edits =
                [| { Range = diagnostic.Range
                     NewText = "=" } |]
              Kind = FixKind.Fix } ]
      | _ -> return []
    })

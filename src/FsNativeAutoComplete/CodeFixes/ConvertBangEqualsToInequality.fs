/// fix to convert uses of != to <> to use the proper thing
module FsNativeAutoComplete.CodeFix.ConvertBangEqualsToInequality

open FsToolkit.ErrorHandling
open FsNativeAutoComplete.CodeFix.Types
open FsNativeAutoComplete
open FsNativeAutoComplete.LspHelpers

let title = "Use <> for inequality check"

let fix (getRangeText: GetRangeText) : CodeFix =
  Run.ifDiagnosticByCode (Set.ofList [ "43" ]) (fun diag codeActionParams ->
    asyncResult {
      let fileName = codeActionParams.TextDocument.GetFilePath() |> Utils.normalizePath

      let! errorText = getRangeText fileName diag.Range
      do! Result.guard (fun () -> errorText = "!=") "Not an != equality usage"

      return
        [ { Title = title
            File = codeActionParams.TextDocument
            SourceDiagnostic = Some diag
            Kind = FixKind.Fix
            Edits = [| { Range = diag.Range; NewText = "<>" } |] } ]

    })

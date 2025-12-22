module FsNativeAutoComplete.CodeFix.RemoveUnusedOpens

open Ionide.LanguageServerProtocol.Types
open FsNativeAutoComplete.CodeFix
open FsNativeAutoComplete.CodeFix.Types
open FsToolkit.ErrorHandling
open FsNativeAutoComplete
open FsNativeAutoComplete.LspHelpers
open FsNativeAutoComplete.CodeFix.Navigation

let title = "Remove unused open"

/// a codefix that removes unused open statements from the source
let fix (getFileLines: GetFileLines) : CodeFix =
  Run.ifDiagnosticByMessage "Unused open statement" (fun d codeActionParams ->
    asyncResult {
      let fileName = codeActionParams.TextDocument.GetFilePath() |> Utils.normalizePath

      let! lines = getFileLines fileName

      let lineToRemove = d.Range.Start.Line
      let range = lines |> rangeToDeleteFullLine lineToRemove

      return
        [ { Edits = [| { Range = range; NewText = "" } |]
            File = codeActionParams.TextDocument
            Title = title
            SourceDiagnostic = Some d
            Kind = FixKind.Refactor } ]
    })

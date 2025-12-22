module FsNativeAutoComplete.CodeFix.ConvertDoubleEqualsToSingleEquals

open FsToolkit.ErrorHandling
open FsNativeAutoComplete.CodeFix.Types
open FsNativeAutoComplete
open FsNativeAutoComplete.LspHelpers

val title: string

/// a codefix that corrects == equality to = equality
val fix:
  getRangeText: GetRangeText ->
    (Ionide.LanguageServerProtocol.Types.CodeActionParams -> Async<Result<Fix list, string>>)

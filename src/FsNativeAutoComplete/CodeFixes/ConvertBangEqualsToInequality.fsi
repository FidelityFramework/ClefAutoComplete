/// fix to convert uses of != to <> to use the proper thing
module FsNativeAutoComplete.CodeFix.ConvertBangEqualsToInequality

open FsToolkit.ErrorHandling
open FsNativeAutoComplete.CodeFix.Types
open FsNativeAutoComplete
open FsNativeAutoComplete.LspHelpers

val title: string

val fix:
  getRangeText: GetRangeText ->
    (Ionide.LanguageServerProtocol.Types.CodeActionParams -> Async<Result<Fix list, string>>)

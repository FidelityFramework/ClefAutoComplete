module FsNativeAutoComplete.CodeFix.ChangePrefixNegationToInfixSubtraction

open FsToolkit.ErrorHandling
open FsNativeAutoComplete.CodeFix.Navigation
open FsNativeAutoComplete.CodeFix.Types
open Ionide.LanguageServerProtocol.Types
open FsNativeAutoComplete
open FsNativeAutoComplete.LspHelpers

val title: string
/// a codefix that corrects -<something> to - <something> when negation is not intended
val fix: getFileLines: GetFileLines -> (CodeActionParams -> Async<Result<Fix list, string>>)

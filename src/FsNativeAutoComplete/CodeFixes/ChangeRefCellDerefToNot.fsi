module FsNativeAutoComplete.CodeFix.ChangeRefCellDerefToNot

open FsToolkit.ErrorHandling
open FsNativeAutoComplete.CodeFix.Types
open Ionide.LanguageServerProtocol.Types
open FsNativeAutoComplete
open FsNativeAutoComplete.LspHelpers

val title: string
/// a codefix that changes a ref cell deref (!) to a call to 'not'
val fix: getParseResultsForFile: GetParseResultsForFile -> (CodeActionParams -> Async<Result<Fix list, string>>)

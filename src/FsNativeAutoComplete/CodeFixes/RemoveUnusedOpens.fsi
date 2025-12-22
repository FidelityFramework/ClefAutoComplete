module FsNativeAutoComplete.CodeFix.RemoveUnusedOpens

open Ionide.LanguageServerProtocol.Types
open FsNativeAutoComplete.CodeFix
open FsNativeAutoComplete.CodeFix.Types
open FsToolkit.ErrorHandling
open FsNativeAutoComplete
open FsNativeAutoComplete.LspHelpers
open FsNativeAutoComplete.CodeFix.Navigation

val title: string
/// a codefix that removes unused open statements from the source
val fix: getFileLines: GetFileLines -> (CodeActionParams -> Async<Result<Fix list, string>>)

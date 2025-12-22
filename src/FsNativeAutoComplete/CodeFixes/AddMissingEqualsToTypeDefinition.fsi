module FsNativeAutoComplete.CodeFix.AddMissingEqualsToTypeDefinition

open FsToolkit.ErrorHandling
open FsNativeAutoComplete.CodeFix.Navigation
open FsNativeAutoComplete.CodeFix.Types
open Ionide.LanguageServerProtocol.Types
open FsNativeAutoComplete
open FsNativeAutoComplete.LspHelpers

val title: string
/// a codefix that adds in missing '=' characters in type declarations
val fix: getFileLines: GetFileLines -> (CodeActionParams -> Async<Result<Fix list, string>>)

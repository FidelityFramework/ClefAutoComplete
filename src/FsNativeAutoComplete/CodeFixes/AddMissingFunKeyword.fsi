module FsNativeAutoComplete.CodeFix.AddMissingFunKeyword

open FsToolkit.ErrorHandling
open FsNativeAutoComplete.CodeFix.Navigation
open FsNativeAutoComplete.CodeFix.Types
open Ionide.LanguageServerProtocol.Types
open FsNativeAutoComplete
open FsNativeAutoComplete.LspHelpers

val title: string
/// a codefix that adds a missing 'fun' keyword to a lambda
val fix: getFileLines: GetFileLines -> getLineText: GetLineText -> (CodeActionParams -> Async<Result<Fix list, string>>)

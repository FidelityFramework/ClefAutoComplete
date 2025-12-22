module FsNativeAutoComplete.CodeFix.AddMissingRecKeyword

open FsToolkit.ErrorHandling
open FsNativeAutoComplete.CodeFix.Navigation
open FsNativeAutoComplete.CodeFix.Types
open Ionide.LanguageServerProtocol.Types
open FsNativeAutoComplete
open FsNativeAutoComplete.LspHelpers
open FSharp.UMX

val title: symbolName: 'a -> string
/// a codefix that adds the 'rec' modifier to a binding in a mutually-recursive loop
val fix: getFileLines: GetFileLines -> getLineText: GetLineText -> (CodeActionParams -> Async<Result<Fix list, string>>)

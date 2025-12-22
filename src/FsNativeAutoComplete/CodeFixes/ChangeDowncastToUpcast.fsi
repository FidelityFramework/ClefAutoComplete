module FsNativeAutoComplete.CodeFix.ChangeDowncastToUpcast

open FsToolkit.ErrorHandling
open FsNativeAutoComplete.CodeFix.Types
open Ionide.LanguageServerProtocol.Types
open FsNativeAutoComplete
open FsNativeAutoComplete.LspHelpers

val titleUpcastOperator: string
val titleUpcastFunction: string
/// a codefix that replaces unsafe casts with safe casts
val fix: getRangeText: GetRangeText -> (CodeActionParams -> Async<Result<Fix list, string>>)

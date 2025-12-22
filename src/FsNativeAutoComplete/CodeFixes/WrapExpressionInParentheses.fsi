module FsNativeAutoComplete.CodeFix.WrapExpressionInParentheses

open FsToolkit.ErrorHandling
open FsNativeAutoComplete.CodeFix.Types
open Ionide.LanguageServerProtocol.Types
open FsNativeAutoComplete

val title: string
/// a codefix that parenthesizes a member expression that needs it
val fix: (CodeActionParams -> Async<Result<Fix list, string>>)

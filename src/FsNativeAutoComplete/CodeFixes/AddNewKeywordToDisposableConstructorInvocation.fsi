module FsNativeAutoComplete.CodeFix.AddNewKeywordToDisposableConstructorInvocation

open FsNativeAutoComplete.CodeFix.Types
open Ionide.LanguageServerProtocol.Types

val title: string

/// a codefix that suggests using the 'new' keyword on IDisposables
val fix: getParseResultsForFile: GetParseResultsForFile -> (CodeActionParams -> Async<Result<Fix list, string>>)

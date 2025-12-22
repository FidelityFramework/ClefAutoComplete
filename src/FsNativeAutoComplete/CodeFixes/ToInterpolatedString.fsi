module FsNativeAutoComplete.CodeFix.ToInterpolatedString

open FsNativeAutoComplete.CodeFix.Types
open Ionide.LanguageServerProtocol.Types

val title: string

val fix:
  getParseResultsForFile: GetParseResultsForFile ->
  getLanguageVersion: GetLanguageVersion ->
  codeActionParams: CodeActionParams ->
    Async<Result<Fix list, string>>

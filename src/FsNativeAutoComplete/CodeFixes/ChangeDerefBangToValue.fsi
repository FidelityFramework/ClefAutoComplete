/// replace use of ! operator on ref cells with calls to .Value
module FsNativeAutoComplete.CodeFix.ChangeDerefBangToValue

open FsToolkit.ErrorHandling
open FsNativeAutoComplete.CodeFix.Types
open FsNativeAutoComplete
open FsNativeAutoComplete.LspHelpers
open FSharp.Compiler.Syntax
open FSharp.Compiler.Text.Range
open FSharp.UMX

val title: string

val fix:
  getParseResultsForFile: GetParseResultsForFile ->
    (Ionide.LanguageServerProtocol.Types.CodeActionParams -> Async<Result<Fix list, string>>)

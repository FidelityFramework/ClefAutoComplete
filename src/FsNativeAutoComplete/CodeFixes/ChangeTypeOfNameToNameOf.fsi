/// a codefix that replaces typeof<'t>.Name with nameof('t)
module FsNativeAutoComplete.CodeFix.ChangeTypeOfNameToNameOf

open FsToolkit.ErrorHandling
open FsNativeAutoComplete.CodeFix.Types
open FsNativeAutoComplete
open FsNativeAutoComplete.LspHelpers
open FSharp.Compiler.CodeAnalysis
open FSharp.Compiler.Syntax

val title: string

val fix:
  getParseResultsForFile: GetParseResultsForFile ->
    (Ionide.LanguageServerProtocol.Types.CodeActionParams -> Async<Result<Fix list, string>>)

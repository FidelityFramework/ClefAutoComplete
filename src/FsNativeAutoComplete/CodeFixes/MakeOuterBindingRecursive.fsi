/// a codefix that makes a binding 'rec' if something inside the binding requires recursive access
module FsNativeAutoComplete.CodeFix.MakeOuterBindingRecursive

open FsToolkit.ErrorHandling
open FsNativeAutoComplete.CodeFix.Types
open Ionide.LanguageServerProtocol.Types
open FsNativeAutoComplete
open FsNativeAutoComplete.LspHelpers

val title: string

val fix:
  getParseResultsForFile: GetParseResultsForFile ->
  getLineText: GetLineText ->
    (CodeActionParams -> Async<Result<Fix list, string>>)

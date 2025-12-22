module FsNativeAutoComplete.CodeFix.MakeDeclarationMutable

open FsToolkit.ErrorHandling
open FsNativeAutoComplete.CodeFix.Types
open Ionide.LanguageServerProtocol.Types
open FsNativeAutoComplete
open FsNativeAutoComplete.LspHelpers
open FSharp.UMX

val title: string

/// a codefix that makes a binding mutable when a user attempts to mutably set it
val fix:
  getParseResultsForFile: GetParseResultsForFile ->
  getProjectOptionsForFile: GetProjectOptionsForFile ->
    (CodeActionParams -> Async<Result<Fix list, string>>)

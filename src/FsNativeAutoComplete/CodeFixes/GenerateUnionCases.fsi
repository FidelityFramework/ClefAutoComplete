module FsNativeAutoComplete.CodeFix.GenerateUnionCases

open FsToolkit.ErrorHandling
open FsNativeAutoComplete.CodeFix
open FsNativeAutoComplete.CodeFix.Types
open Ionide.LanguageServerProtocol.Types
open FsNativeAutoComplete
open FsNativeAutoComplete.LspHelpers
open FsNativeAutoComplete.CodeFix.Navigation

val title: string

/// a codefix that generates union cases for an incomplete match expression
val fix:
  getFileLines: GetFileLines ->
  getParseResultsForFile: GetParseResultsForFile ->
  generateCases: (ParseAndCheckResults -> FcsPos -> IFSACSourceText -> Async<CoreResponse<string * FcsPos>>) ->
  getTextReplacements: (unit -> Map<string, string>) ->
    (CodeActionParams -> Async<Result<Fix list, string>>)

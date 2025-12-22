module FsNativeAutoComplete.CodeFix.GenerateRecordStub

open FsToolkit.ErrorHandling
open FsNativeAutoComplete.CodeFix.Types
open Ionide.LanguageServerProtocol.Types
open FsNativeAutoComplete
open FsNativeAutoComplete.LspHelpers

val title: string

/// a codefix that generates member stubs for a record declaration
val fix:
  getParseResultsForFile: GetParseResultsForFile ->
  genRecordStub: (ParseAndCheckResults -> FcsPos -> IFSACSourceText -> Async<CoreResponse<string * FcsPos>>) ->
  getTextReplacements: (unit -> Map<string, string>) ->
    (CodeActionParams -> Async<Result<Fix list, string>>)

module FsNativeAutoComplete.CodeFix.GenerateAbstractClassStub

open FsToolkit.ErrorHandling
open FsNativeAutoComplete.CodeFix
open FsNativeAutoComplete.CodeFix.Types
open Ionide.LanguageServerProtocol.Types
open FsNativeAutoComplete
open FsNativeAutoComplete.LspHelpers
open FSharp.UMX

val title: string

/// a codefix that generates stubs for required override members in abstract types
val fix:
  getParseResultsForFile: GetParseResultsForFile ->
  genAbstractClassStub:
    (ParseAndCheckResults -> FcsRange -> IFSACSourceText -> string -> Async<CoreResponse<FcsPos * string>>) ->
  getTextReplacements: (unit -> Map<string, string>) ->
    (CodeActionParams -> Async<Result<Fix list, string>>)

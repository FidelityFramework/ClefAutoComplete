module FsNativeAutoComplete.CodeFix.ResolveNamespace

open Ionide.LanguageServerProtocol.Types
open FsNativeAutoComplete.CodeFix
open FsNativeAutoComplete.CodeFix.Types
open FsToolkit.ErrorHandling
open FsNativeAutoComplete.LspHelpers
open FsNativeAutoComplete
open FSharp.Compiler.Text
open FSharp.Compiler.EditorServices

type LineText = string

/// a codefix the provides suggestions for opening modules or using qualified names when an identifier is found that needs qualification
val fix:
  getParseResultsForFile: GetParseResultsForFile ->
  getNamespaceSuggestions:
    (ParseAndCheckResults
      -> FcsPos
      -> LineText
      -> Async<CoreResponse<string * list<string * string * InsertionContext * bool> * list<string * string>>>) ->
    (CodeActionParams -> Async<Result<Fix list, string>>)

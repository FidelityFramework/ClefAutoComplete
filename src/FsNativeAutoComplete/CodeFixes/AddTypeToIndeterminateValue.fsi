module FsNativeAutoComplete.CodeFix.AddTypeToIndeterminateValue

open FsToolkit.ErrorHandling
open FsNativeAutoComplete.CodeFix.Types
open Ionide.LanguageServerProtocol.Types
open FsNativeAutoComplete
open FsNativeAutoComplete.LspHelpers
open FSharp.Compiler.EditorServices
open FSharp.Compiler.Symbols
open FSharp.UMX

val title: string

/// fix indeterminate type errors by adding an explicit type to a value
val fix:
  getParseResultsForFile: GetParseResultsForFile ->
  getProjectOptionsForFile: GetProjectOptionsForFile ->
    (CodeActionParams -> Async<Result<Fix list, string>>)

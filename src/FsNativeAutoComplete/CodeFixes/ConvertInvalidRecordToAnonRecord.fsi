module FsNativeAutoComplete.CodeFix.ConvertInvalidRecordToAnonRecord

open FsToolkit.ErrorHandling
open FsNativeAutoComplete.CodeFix.Types
open Ionide.LanguageServerProtocol.Types
open FsNativeAutoComplete
open FsNativeAutoComplete.CodeFix.Navigation
open FsNativeAutoComplete.LspHelpers

val title: string
/// a codefix that converts unknown/partial record expressions to anonymous records
val fix: getParseResultsForFile: GetParseResultsForFile -> (CodeActionParams -> Async<Result<Fix list, string>>)

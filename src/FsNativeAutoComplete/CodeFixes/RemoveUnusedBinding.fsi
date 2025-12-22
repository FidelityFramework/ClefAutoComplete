module FsNativeAutoComplete.CodeFix.RemoveUnusedBinding

open FsToolkit.ErrorHandling
open FsNativeAutoComplete.CodeFix.Navigation
open FsNativeAutoComplete.CodeFix.Types
open Ionide.LanguageServerProtocol.Types
open FsNativeAutoComplete
open FsNativeAutoComplete.LspHelpers
open FSharp.Compiler.CodeAnalysis
open FSharp.Compiler.Syntax
open FSharp.Compiler.Text

val titleParameter: string
val titleBinding: string
val fix: getParseResults: GetParseResultsForFile -> (CodeActionParams -> Async<Result<Fix list, string>>)

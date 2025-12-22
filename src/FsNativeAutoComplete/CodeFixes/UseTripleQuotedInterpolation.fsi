module FsNativeAutoComplete.CodeFix.UseTripleQuotedInterpolation

open FsToolkit.ErrorHandling
open FsNativeAutoComplete.CodeFix.Types
open Ionide.LanguageServerProtocol.Types
open FsNativeAutoComplete
open FsNativeAutoComplete.LspHelpers
open FsNativeAutoComplete.FCSPatches

val title: string

/// a codefix that replaces erroring single-quoted interpolations with triple-quoted interpolations
val fix: getParseResultsForFile: GetParseResultsForFile -> (CodeActionParams -> Async<Result<Fix list, string>>)

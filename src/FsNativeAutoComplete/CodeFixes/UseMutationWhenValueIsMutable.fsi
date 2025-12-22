module FsNativeAutoComplete.CodeFix.UseMutationWhenValueIsMutable

open FsToolkit.ErrorHandling
open FsNativeAutoComplete.CodeFix.Types
open Ionide.LanguageServerProtocol.Types
open FsNativeAutoComplete
open FsNativeAutoComplete.CodeFix.Navigation
open FsNativeAutoComplete.LspHelpers
open FSharp.Compiler.Symbols

val title: string
/// a codefix that changes equality checking to mutable assignment when the compiler thinks it's relevant
val fix: getParseResultsForFile: GetParseResultsForFile -> (CodeActionParams -> Async<Result<Fix list, string>>)

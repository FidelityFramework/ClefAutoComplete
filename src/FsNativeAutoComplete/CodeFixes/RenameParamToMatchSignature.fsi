module FsNativeAutoComplete.CodeFix.RenameParamToMatchSignature

open FsToolkit.ErrorHandling
open FsNativeAutoComplete.CodeFix.Types
open Ionide.LanguageServerProtocol.Types
open FsNativeAutoComplete
open FsNativeAutoComplete.LspHelpers
open FSharp.Compiler.Syntax

val title: name: string -> string
/// codefix that renames a parameter to match its signature (specified in fsi file)
val fix: getParseResultsForFile: GetParseResultsForFile -> (CodeActionParams -> Async<Result<Fix list, string>>)

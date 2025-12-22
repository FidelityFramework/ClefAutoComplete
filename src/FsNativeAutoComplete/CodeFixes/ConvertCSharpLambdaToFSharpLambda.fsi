/// a codefix that rewrites C#-style '=>' lambdas to F#-style 'fun _ -> _' lambdas
module FsNativeAutoComplete.CodeFix.ConvertCSharpLambdaToFSharpLambda

open FsToolkit.ErrorHandling
open FsNativeAutoComplete.CodeFix.Types
open Ionide.LanguageServerProtocol.Types
open FsNativeAutoComplete
open FsNativeAutoComplete.LspHelpers
open FSharp.Compiler.Syntax
open FSharp.Compiler.Text

val title: string

val fix:
  getParseResultsForFile: GetParseResultsForFile ->
  getLineText: GetLineText ->
    (CodeActionParams -> Async<Result<Fix list, string>>)

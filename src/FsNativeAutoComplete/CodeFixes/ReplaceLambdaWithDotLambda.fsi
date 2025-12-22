module FsNativeAutoComplete.CodeFix.ReplaceLambdaWithDotLambda

open FsNativeAutoComplete.CodeFix.Types

val titleReplaceToDotLambda: string
val titleReplaceToLambda: string
val fix: getLanguageVersion: GetLanguageVersion -> getParseResultsForFile: GetParseResultsForFile -> CodeFix

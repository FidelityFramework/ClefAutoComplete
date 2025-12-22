module FsNativeAutoComplete.CodeFix.UpdateTypeAbbreviationInSignatureFile

open FsNativeAutoComplete.CodeFix.Types

val title: string
val fix: getParseResultsForFile: GetParseResultsForFile -> CodeFix

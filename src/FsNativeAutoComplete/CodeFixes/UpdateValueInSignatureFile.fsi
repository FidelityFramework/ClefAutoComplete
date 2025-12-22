module FsNativeAutoComplete.CodeFix.UpdateValueInSignatureFile

open FsNativeAutoComplete.CodeFix.Types

val title: string
val fix: getParseResultsForFile: GetParseResultsForFile -> CodeFix

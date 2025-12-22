module FsNativeAutoComplete.CodeFix.AddBindingToSignatureFile

open FsNativeAutoComplete.CodeFix.Types

val title: string
val fix: getProjectOptionsForFile: GetProjectOptionsForFile -> getParseResultsForFile: GetParseResultsForFile -> CodeFix

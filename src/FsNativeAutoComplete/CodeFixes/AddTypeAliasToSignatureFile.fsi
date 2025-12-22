module FsNativeAutoComplete.CodeFix.AddTypeAliasToSignatureFile

open FsNativeAutoComplete.CodeFix.Types

val title: string
val fix: getProjectOptionsForFile: GetProjectOptionsForFile -> getParseResultsForFile: GetParseResultsForFile -> CodeFix

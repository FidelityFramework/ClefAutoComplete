module FsNativeAutoComplete.CodeFix.IgnoreExpression

open FsNativeAutoComplete.CodeFix.Types

val title: string
val fix: getParseResultsForFile: GetParseResultsForFile -> CodeFix

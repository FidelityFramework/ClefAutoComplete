module FsNativeAutoComplete.CodeFix.ExprTypeMismatch

open FsNativeAutoComplete.CodeFix.Types

val title: string
val fix: getParseResultsForFile: GetParseResultsForFile -> CodeFix

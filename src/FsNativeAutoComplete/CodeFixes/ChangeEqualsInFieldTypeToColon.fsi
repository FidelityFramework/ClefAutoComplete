module FsNativeAutoComplete.CodeFix.ChangeEqualsInFieldTypeToColon

open FsToolkit.ErrorHandling
open FsNativeAutoComplete.CodeFix.Types
open FsNativeAutoComplete

val title: string
/// a codefix that fixes a malformed record type annotation to use colon instead of equals
val fix: CodeFix

module FsNativeAutoComplete.CodeFix.AddMissingInstanceMember

open FsToolkit.ErrorHandling
open FsNativeAutoComplete.CodeFix.Types
open Ionide.LanguageServerProtocol.Types

val title: string
val fix: (CodeActionParams -> Async<Result<Fix list, string>>)

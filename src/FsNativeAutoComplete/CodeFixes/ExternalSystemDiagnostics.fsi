module FsNativeAutoComplete.CodeFix.ExternalSystemDiagnostics

open FsToolkit.ErrorHandling
open FsNativeAutoComplete.CodeFix
open FsNativeAutoComplete.CodeFix.Types
open Ionide.LanguageServerProtocol.Types
open FsNativeAutoComplete
open Newtonsoft.Json.Linq

/// a codefix that generates fixes reported by FSharpLint
val linter: (CodeActionParams -> Async<Result<Fix list, string>>)
/// a codefix that generates fixes reported by F# Analyzers
val analyzers: (CodeActionParams -> Async<Result<Fix list, string>>)

module Utils.Tests.CursorbasedTests

open Expecto
open Helpers
open Ionide.LanguageServerProtocol.Types
open Utils.Utils
open Utils.ServerTests
open Utils.CursorbasedTests

val tests: state: (unit -> FsNativeAutoComplete.Lsp.IFSharpLspServer * System.IObservable<string * obj>) -> Test

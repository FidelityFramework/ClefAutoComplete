module Utils.Tests.Server

open System
open Expecto
open Helpers
open FsNativeAutoComplete
open FsNativeAutoComplete.Lsp
open FsNativeAutoComplete.LspHelpers
open Ionide.LanguageServerProtocol
open Ionide.LanguageServerProtocol.Types
open Utils.ServerTests
open Utils.Server
open Utils.Utils
open FsToolkit.ErrorHandling
open FSharpx.Control

val tests: state: (unit -> IFSharpLspServer * IObservable<string * obj>) -> Test

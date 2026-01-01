/// Native F# LSP Server Interface
/// Handles .fidproj projects and .fsnx scripts using FNCS.
/// This is the native path of the unified FSNAC language server.
namespace FsNativeAutoComplete.Lsp

open System
open Ionide.LanguageServerProtocol.Types

/// LSP server for native F# projects (.fidproj, .fsnx)
type AdaptiveFSharpNativeLspServer =
    new: lspClient: FSharpLspClient -> AdaptiveFSharpNativeLspServer

    // Lifecycle
    member Initialize: rootPath: string option -> unit

    // Document Synchronization
    member TextDocumentDidOpen: p: DidOpenTextDocumentParams -> unit
    member TextDocumentDidChange: p: DidChangeTextDocumentParams -> unit
    member TextDocumentDidClose: p: DidCloseTextDocumentParams -> unit
    member TextDocumentDidSave: p: DidSaveTextDocumentParams -> unit

    // Language Features
    member TextDocumentHover: p: HoverParams -> Async<Ionide.LanguageServerProtocol.JsonRpc.LspResult<Hover option>>
    member TextDocumentCompletion: p: CompletionParams -> Async<Ionide.LanguageServerProtocol.JsonRpc.LspResult<CompletionList option>>

    // Workspace Features
    member WorkspaceDidChangeWatchedFiles: p: DidChangeWatchedFilesParams -> unit

    // Server Capabilities
    static member GetCapabilities: unit -> ServerCapabilities

    // Utility
    member IsNativeFile: filePath: string -> bool
    member State: NativeState

    interface IDisposable

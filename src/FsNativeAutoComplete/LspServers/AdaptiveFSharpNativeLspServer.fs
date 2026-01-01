/// Native F# LSP Server
/// Handles .fidproj projects and .fsnx scripts using FNCS.
/// This is the native path of the unified FSNAC language server.
namespace FsNativeAutoComplete.Lsp

open System
open System.IO
open FsNativeAutoComplete.Core
open FsNativeAutoComplete.Core.ProjectKind
open FsNativeAutoComplete.Core.FidprojLoader
open FsNativeAutoComplete.Core.NativeCompilerServiceInterface
open FsNativeAutoComplete.Core.FsniDirectives
open FsNativeAutoComplete.Logging
open Ionide.LanguageServerProtocol
open Ionide.LanguageServerProtocol.Server
open Ionide.LanguageServerProtocol.Types
open Ionide.LanguageServerProtocol.JsonRpc
open FSharp.UMX
open FsNativeAutoComplete.LspHelpers
open IcedTasks

// =============================================================================
// Native LSP Server
// =============================================================================

/// LSP server for native F# projects (.fidproj, .fsnx)
type AdaptiveFSharpNativeLspServer(lspClient: FSharpLspClient) =

    let logger = LogProvider.getLoggerFor<AdaptiveFSharpNativeLspServer> ()

    // Native state management
    let nativeState = NativeState(lspClient)

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    let getFilePath (uri: DocumentUri) : string =
        let path = uri.Replace("file://", "")
        // Handle Windows paths
        if path.StartsWith("/") && path.Length > 2 && path.[2] = ':' then
            path.Substring(1)
        else
            path

    let getFilePathFromTextDoc (td: TextDocumentIdentifier) : string =
        getFilePath td.Uri

    let getPositionFromPos (p: Position) : int * int =
        int p.Line, int p.Character

    // -------------------------------------------------------------------------
    // Lifecycle
    // -------------------------------------------------------------------------

    /// Initialize the server
    member _.Initialize(rootPath: string option) =
        logger.info (
            Log.setMessage "AdaptiveFSharpNativeLspServer.Initialize"
            >> Log.addContextDestructured "rootPath" rootPath
        )

        // Discover and load native projects and scripts
        match rootPath with
        | Some root when Directory.Exists(root) ->
            // Load .fidproj projects
            let projects = nativeState.DiscoverProjects(root)
            for projPath in projects do
                match nativeState.LoadProject(projPath) with
                | Ok proj ->
                    logger.info (
                        Log.setMessage "Loaded native project: {project}"
                        >> Log.addContextDestructured "project" proj.Options.Name
                    )
                | Error e ->
                    logger.error (
                        Log.setMessage "Failed to load native project: {error}"
                        >> Log.addContextDestructured "error" e
                    )

            // Load .fsnx scripts
            let scripts = nativeState.DiscoverScripts(root)
            for scriptPath in scripts do
                match nativeState.LoadScript(scriptPath) with
                | Ok script ->
                    logger.info (
                        Log.setMessage "Loaded native script: {script}"
                        >> Log.addContextDestructured "script" script.Options.ScriptPath
                    )
                | Error e ->
                    logger.error (
                        Log.setMessage "Failed to load native script: {error}"
                        >> Log.addContextDestructured "error" e
                    )
        | _ -> ()

    // -------------------------------------------------------------------------
    // Document Synchronization
    // -------------------------------------------------------------------------

    /// Handle document open
    member _.TextDocumentDidOpen(p: DidOpenTextDocumentParams) =
        let filePath = getFilePath p.TextDocument.Uri
        logger.info (
            Log.setMessage "TextDocumentDidOpen: {path}"
            >> Log.addContextDestructured "path" filePath
        )

        if nativeState.IsNativeFile(filePath) then
            // Auto-load scripts if not already loaded
            if nativeState.IsScript(filePath) then
                match nativeState.GetScript(filePath) with
                | None ->
                    // Load the script from the opened document
                    match nativeState.LoadScript(filePath) with
                    | Ok _ ->
                        logger.info (
                            Log.setMessage "Auto-loaded script: {path}"
                            >> Log.addContextDestructured "path" filePath
                        )
                    | Error e ->
                        logger.warn (
                            Log.setMessage "Failed to load script: {path}, {error}"
                            >> Log.addContextDestructured "path" filePath
                            >> Log.addContextDestructured "error" e
                        )
                | Some _ -> ()

            nativeState.OpenDocument(filePath, p.TextDocument.Text, int p.TextDocument.Version)

            // Trigger initial check and publish diagnostics
            let diagnostics = nativeState.GetDiagnostics(filePath)
            let lspDiagnostics = NativeDiagnostics.toLspDiagnostics diagnostics
            lspClient.TextDocumentPublishDiagnostics {
                Uri = p.TextDocument.Uri
                Version = Some p.TextDocument.Version
                Diagnostics = lspDiagnostics
            } |> Async.Start

    /// Handle document change
    member _.TextDocumentDidChange(p: DidChangeTextDocumentParams) =
        let filePath = getFilePath p.TextDocument.Uri
        logger.info (
            Log.setMessage "TextDocumentDidChange: {path}"
            >> Log.addContextDestructured "path" filePath
        )

        if nativeState.IsNativeFile(filePath) then
            // Get the full text (assuming full sync for now)
            match p.ContentChanges |> Array.tryLast with
            | Some change ->
                match change with
                | U2.C2 fullChange ->
                    nativeState.UpdateDocument(filePath, fullChange.Text, int p.TextDocument.Version)
                    // Also update script state if this is a script
                    if nativeState.IsScript(filePath) then
                        nativeState.UpdateScript(filePath, fullChange.Text)
                | U2.C1 _ ->
                    // Incremental change - for now, we'd need full text
                    // This is a simplification; real implementation would apply incremental changes
                    ()
            | None -> ()

            // Re-check and publish diagnostics
            let diagnostics = nativeState.GetDiagnostics(filePath)
            let lspDiagnostics = NativeDiagnostics.toLspDiagnostics diagnostics
            lspClient.TextDocumentPublishDiagnostics {
                Uri = p.TextDocument.Uri
                Version = Some p.TextDocument.Version
                Diagnostics = lspDiagnostics
            } |> Async.Start

    /// Handle document close
    member _.TextDocumentDidClose(p: DidCloseTextDocumentParams) =
        let filePath = getFilePath p.TextDocument.Uri
        logger.info (
            Log.setMessage "TextDocumentDidClose: {path}"
            >> Log.addContextDestructured "path" filePath
        )

        if nativeState.IsNativeFile(filePath) then
            nativeState.CloseDocument(filePath)

    /// Handle document save
    member _.TextDocumentDidSave(p: DidSaveTextDocumentParams) =
        let filePath = getFilePath p.TextDocument.Uri
        logger.info (
            Log.setMessage "TextDocumentDidSave: {path}"
            >> Log.addContextDestructured "path" filePath
        )

        if nativeState.IsNativeFile(filePath) then
            // Update source if provided
            match p.Text with
            | Some text ->
                nativeState.UpdateDocument(filePath, text, 0)
            | None -> ()

            // Re-check and publish diagnostics
            let diagnostics = nativeState.GetDiagnostics(filePath)
            let lspDiagnostics = NativeDiagnostics.toLspDiagnostics diagnostics
            lspClient.TextDocumentPublishDiagnostics {
                Uri = p.TextDocument.Uri
                Version = None
                Diagnostics = lspDiagnostics
            } |> Async.Start

    // -------------------------------------------------------------------------
    // Language Features
    // -------------------------------------------------------------------------

    /// Handle hover request
    member _.TextDocumentHover(p: HoverParams) : Async<LspResult<Hover option>> =
        async {
            let filePath = getFilePathFromTextDoc p.TextDocument
            let line, col = getPositionFromPos p.Position

            logger.info (
                Log.setMessage "TextDocumentHover: {path} @ {line}:{col}"
                >> Log.addContextDestructured "path" filePath
                >> Log.addContextDestructured "line" line
                >> Log.addContextDestructured "col" col
            )

            if nativeState.IsNativeFile(filePath) then
                match nativeState.GetHoverInfo(filePath, line, col) with
                | Some hover ->
                    return LspResult.success (Some (NativeDiagnostics.toLspHover hover))
                | None ->
                    return LspResult.success None
            else
                return LspResult.success None
        }

    /// Handle completion request
    member _.TextDocumentCompletion(p: CompletionParams) : Async<LspResult<CompletionList option>> =
        async {
            let filePath = getFilePathFromTextDoc p.TextDocument
            let line, col = getPositionFromPos p.Position

            logger.info (
                Log.setMessage "TextDocumentCompletion: {path} @ {line}:{col}"
                >> Log.addContextDestructured "path" filePath
                >> Log.addContextDestructured "line" line
                >> Log.addContextDestructured "col" col
            )

            if nativeState.IsNativeFile(filePath) then
                let items = nativeState.GetCompletions(filePath, line, col)
                let lspItems = NativeDiagnostics.toLspCompletionItems items

                return LspResult.success (Some {
                    IsIncomplete = false
                    Items = lspItems
                    ItemDefaults = None
                })
            else
                return LspResult.success None
        }

    // -------------------------------------------------------------------------
    // Workspace Features
    // -------------------------------------------------------------------------

    /// Handle workspace/didChangeWatchedFiles
    member _.WorkspaceDidChangeWatchedFiles(p: DidChangeWatchedFilesParams) =
        for change in p.Changes do
            let filePath = getFilePath change.Uri

            logger.info (
                Log.setMessage "WorkspaceDidChangeWatchedFiles: {path} {type}"
                >> Log.addContextDestructured "path" filePath
                >> Log.addContextDestructured "type" change.Type
            )

            // Handle .fidproj file changes
            if filePath.EndsWith(".fidproj") then
                match change.Type with
                | FileChangeType.Created
                | FileChangeType.Changed ->
                    match nativeState.LoadProject(filePath) with
                    | Ok _ -> ()
                    | Error e ->
                        logger.error (
                            Log.setMessage "Failed to reload project: {error}"
                            >> Log.addContextDestructured "error" e
                        )
                | FileChangeType.Deleted ->
                    nativeState.UnloadProject(filePath)
                | _ -> () // Handle any other enum values

            // Handle .fsnx script file changes
            elif filePath.EndsWith(".fsnx") then
                match change.Type with
                | FileChangeType.Created
                | FileChangeType.Changed ->
                    match nativeState.LoadScript(filePath) with
                    | Ok _ -> ()
                    | Error e ->
                        logger.error (
                            Log.setMessage "Failed to reload script: {error}"
                            >> Log.addContextDestructured "error" e
                        )
                | FileChangeType.Deleted ->
                    nativeState.UnloadScript(filePath)
                | _ -> () // Handle any other enum values

    // -------------------------------------------------------------------------
    // Server Capabilities
    // -------------------------------------------------------------------------

    /// Get server capabilities for native projects
    static member GetCapabilities() : ServerCapabilities =
        { ServerCapabilities.Default with
            HoverProvider = Some (U2.C1 true)
            CompletionProvider = Some {
                ResolveProvider = Some false
                TriggerCharacters = Some [| "."; " " |]
                AllCommitCharacters = None
                CompletionItem = None
                WorkDoneProgress = None
            }
            TextDocumentSync = Some (U2.C1 {
                OpenClose = Some true
                Change = Some TextDocumentSyncKind.Full
                WillSave = Some false
                WillSaveWaitUntil = Some false
                Save = Some (U2.C2 { IncludeText = Some true })
            })
            DiagnosticProvider = Some (U2.C1 {
                Identifier = Some "fsnative"
                InterFileDependencies = true
                WorkspaceDiagnostics = false
                WorkDoneProgress = None
            })
        }

    // -------------------------------------------------------------------------
    // Utility
    // -------------------------------------------------------------------------

    /// Check if a file should be handled by the native server
    member _.IsNativeFile(filePath: string) : bool =
        nativeState.IsNativeFile(filePath)

    /// Get the native state (for testing/debugging)
    member _.State = nativeState

    interface IDisposable with
        member _.Dispose() =
            logger.info (Log.setMessage "AdaptiveFSharpNativeLspServer.Dispose")

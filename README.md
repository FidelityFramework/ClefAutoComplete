# FsNativeAutoComplete (FSNAC)

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE.md)

<p align="center">
<strong>Native-First IDE Services for F#</strong><br>
<em>Language Server Protocol backend for the Fidelity framework ecosystem.</em>
</p>

## Overview

FsNativeAutoComplete (FSNAC) is a fork of [FsAutoComplete](https://github.com/fsharp/FsAutoComplete) designed to provide rich IDE services for native F# compilation. Where the original FSAC assumes .NET projects with `.fsproj` and NuGet packages, FSNAC understands `.fidproj` manifests, source-based dependencies, and the native type semantics that power the Fidelity framework.

FSNAC is the IDE companion to [FSharpNative Compiler Services (FNCS)](https://github.com/speakeztech/fsnative). Together they provide a complete development experience for native F# compilation: FNCS handles type checking and semantic analysis, while FSNAC delivers that information to your editor.

## The Fidelity Framework

FSNAC is part of the **Fidelity** native F# compilation ecosystem:

| Project | Role |
|---------|------|
| **[Firefly](https://github.com/speakeztech/firefly)** | AOT compiler: F# -> PSG -> MLIR -> Native binary |
| **[FNCS](https://github.com/speakeztech/fsnative)** | F# Native Compiler Services (type checking) |
| **FSNAC** | F# Native AutoComplete (this repository) |
| **[Alloy](https://github.com/speakeztech/alloy)** | Native standard library with platform bindings |
| **[XParsec](https://github.com/speakeztech/xparsec)** | Parser combinators for TOML and PSG traversal |
| **[BAREWire](https://github.com/speakeztech/barewire)** | Binary encoding and zero-copy IPC |
| **[Farscape](https://github.com/speakeztech/farscape)** | C/C++ header parsing for native bindings |

The name "Fidelity" reflects the framework's mission: **preserving type and memory safety** from source through compilation to native execution.

## Why FSNAC Exists

The standard FsAutoComplete does an excellent job for .NET development. But when you're targeting native compilation without a runtime, several assumptions become obstacles:

**Projects are MSBuild XML files.** Native compilation uses `.fidproj` TOML manifests that specify memory models, platform targets, and source-based dependencies.

**Dependencies are NuGet packages.** Fidelity uses source-based distribution through the `fpm` package manager, enabling whole-program optimization across package boundaries.

**Types resolve to BCL.** FNCS provides native semantics for standard F# types: `string` has UTF-8 fat pointer semantics instead of `System.String`, `option` has value semantics instead of heap-allocated reference types.

FSNAC bridges these gaps, providing familiar IDE services while understanding native semantics.

## Roadmap

### ✅ Phase 1: Project Identity and TOML Support (Complete)

FSNAC is established as a distinct, usable tool:

- **Namespace transformation**: Renamed from `FsAutoComplete` to `FsNativeAutoComplete`
- **TOML parsing**: XParsec-based parser for `.fidproj` files
- **FidprojLoader**: Produces `FSharpProjectOptions` from TOML manifests
- **Dual-mode workspace**: Supports `.fidproj` and `.fsproj` in the same workspace

```toml
# Example .fidproj that FSNAC understands
[package]
name = "my_project"
version = "0.1.0"

[dependencies]
alloy = { path = "../alloy/src" }

[build]
sources = ["Program.fs"]
output = "my_project"
output_kind = "console"
```

### ✅ Phase 2: FNCS Integration (Complete)

FSNAC now integrates with FNCS for native type resolution:

- **Native type awareness**: Display native type semantics (string as UTF-8 fat pointer, option as value type) in hover info
- **Dual-mode routing**: Automatically routes LSP requests to FNCS or FCS based on project type
- **Semantic graph navigation**: Uses FNCS SemanticGraph for hover, completions, and go-to-definition
- **Platform binding detection**: Recognizes `Platform.Bindings` module functions

### Phase 3: Advanced Metaprogramming Support

The Fidelity framework leverages F#'s metaprogramming features as first-class compilation infrastructure. FSNAC will provide specialized support for these patterns:

#### Quotations as Semantic Carriers

Quotations (`Expr<'T>`) carry memory constraints and peripheral descriptors through the compilation pipeline. FSNAC will provide:

- Quotation structure visualization
- Navigation from quotation to generated code
- Semantic highlighting for compile-time evaluated expressions

```fsharp
// FSNAC understands this carries peripheral layout information
let gpioQuotation: Expr<PeripheralDescriptor> = <@
    { Name = "GPIO"
      Instances = Map.ofList [("GPIOA", 0x48000000un)]
      MemoryRegion = Peripheral }
@>
```

#### Active Patterns for Structural Recognition

Active patterns enable compositional matching throughout the nanopass pipeline. FSNAC will support:

- Pattern composition visualization
- Navigation to pattern definitions from match sites
- Type flow through partial active patterns

#### Computation Expressions as Control Flow

Computation expressions provide continuation notation that compiles to DCont and Inet dialects. FSNAC will recognize:

- Builder method resolution
- Continuation structure in complex workflows
- Dialect selection hints (sequential vs. parallel)

### Phase 4: Multi-Pane Development

For advanced Fidelity development, FSNAC will support coordinated views:

| Pane | Format | Language Server |
|------|--------|-----------------|
| F# Source | `.fs` | FSNAC |
| MLIR | `.mlir` | mlir-lsp-server |
| LLVM IR | `.ll` | clangd |

This enables tracing code from source through compilation stages.

## Relationship to FsAutoComplete

FSNAC is a fork of the excellent [FsAutoComplete](https://github.com/fsharp/FsAutoComplete) project. We're grateful to the FSAC maintainers and the Ionide community for creating and maintaining the foundation we build upon.

Our modifications focus on project formats and type resolution, not core LSP functionality. The standard LSP endpoints remain compatible, ensuring FSNAC works with existing editor integrations.

## Editor Support

FSNAC provides F# support for any LSP-capable editor:

- **Visual Studio Code** (via Ionide configuration)
- **Neovim** (via nvim-lspconfig)
- **Helix**
- **Emacs** (via eglot or lsp-mode)
- **Kate**
- **Zed**

### Configuration Example (nvim)

```lua
lspconfig.fsautocomplete.setup {
    cmd = { 'dotnet', 'fsnac' },  -- or path to FSNAC binary
    filetypes = { 'fsharp' },
    root_dir = lspconfig.util.root_pattern('*.fidproj', '*.fsproj', '*.sln'),
}

-- Associate .fidproj with TOML syntax
vim.filetype.add({ extension = { fidproj = 'toml' } })
```

## Building

Requirements:
- .NET SDK (see `global.json` for version)

```bash
# Restore tools
dotnet tool restore

# Build
dotnet build

# Test
dotnet test
```

## Supported LSP Features

FSNAC supports the standard LSP endpoints:

- `textDocument/completion` with `completionItem/resolve`
- `textDocument/hover`
- `textDocument/definition`, `typeDefinition`, `implementation`
- `textDocument/references`
- `textDocument/codeAction`, `codeLens`
- `textDocument/formatting` (via Fantomas)
- `textDocument/rename`
- `textDocument/signatureHelp`
- `textDocument/documentSymbol`
- `textDocument/semanticTokens`
- `workspace/symbol`

### Custom Fidelity Endpoints (Planned)

- `fidelity/projectInfo` - Query `.fidproj` configuration
- `fidelity/memoryLayout` - Get type memory layout information
- `fidelity/srtpResolution` - Show SRTP witness resolution
- `fidelity/platformBindings` - List platform binding mappings

## Implementation Status

| Phase | Description | Status |
|-------|-------------|--------|
| Phase 1 | Namespace rename and TOML parsing | ✅ Complete |
| Phase 2 | FNCS integration | ✅ Complete |
| Phase 3 | Metaprogramming support | Future |
| Phase 4 | Multi-pane development | Future |

### What's Working Now

FSNAC can serve as the LSP server for both `.fsproj` and `.fidproj` projects simultaneously:

**For `.fidproj` (native) projects:**
- ✅ TOML project file parsing
- ✅ Workspace loading via `fsharp/workspaceLoad`
- ✅ Hover information with native type semantics
- ✅ Completions from semantic graph
- ✅ Go-to-definition for local bindings
- ✅ Diagnostics publishing on file open/change

**For `.fsproj` (standard) projects:**
- ✅ Full FsAutoComplete functionality preserved
- ✅ MSBuild project loading
- ✅ NuGet package resolution
- ✅ All standard LSP features

The server automatically routes requests to the appropriate backend based on file membership in loaded projects.

## Architecture

```
┌─────────────────────────────────────────────────────────┐
│                        Editor                            │
│              (VS Code, nvim, Helix, etc.)               │
└─────────────────────────┬───────────────────────────────┘
                          │ LSP
┌─────────────────────────▼───────────────────────────────┐
│                        FSNAC                             │
│           FsNativeAutoComplete LSP Server               │
├─────────────────────────────────────────────────────────┤
│  FidprojLoader    │   Standard LSP   │   Custom         │
│  (TOML → Options) │   Handlers       │   Fidelity API   │
└─────────────────────────┬───────────────────────────────┘
                          │
┌─────────────────────────▼───────────────────────────────┐
│                        FNCS                              │
│         FSharpNative Compiler Services                  │
│    (Type checking, SRTP resolution, Native types)       │
└─────────────────────────────────────────────────────────┘
```

## Contributing

Contributions are welcome. Areas of particular interest:

1. **SRTP resolution display** - Showing resolved witness implementations in hover info
2. **Memory layout visualization** - Displaying type memory layouts
3. **Testing with Fidelity projects** - Validating against real native F# code
4. **Editor configuration guides** - Documentation for various editors
5. **Custom endpoint implementation** - Building out `fidelity/*` LSP extensions

## License

This project is licensed under the MIT License - see the [LICENSE.md](LICENSE.md) file for details.

Original FsAutoComplete work is copyright its respective authors. Modifications are copyright SpeakEZ Technologies.

## Acknowledgments

- **[FsAutoComplete Team](https://github.com/fsharp/FsAutoComplete)**: For the excellent LSP foundation
- **[Ionide Project](https://ionide.io/)**: For F# tooling infrastructure
- **Don Syme and F# Contributors**: For creating quotations, active patterns, and computation expressions—the "standing art" that powers native F# compilation

---

*The IDE experience you know. Native semantics under the hood.*

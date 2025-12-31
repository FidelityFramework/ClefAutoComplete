# FsNativeAutoComplete (FSNAC) Agent Instructions

## Project Overview

FsNativeAutoComplete (FSNAC) is a Language Server Protocol (LSP) backend service providing rich editing and IntelliSense features for F# development. It is a fork of [FsAutoComplete](https://github.com/fsharp/FsAutoComplete) extended to support the Fidelity native compilation framework.

**Key Distinction**: FSNAC supports both traditional `.fsproj`/`.sln` projects AND `.fidproj` TOML-based projects for native F# compilation.

## Fidelity Framework Context

FSNAC is part of the **Fidelity** native F# compilation ecosystem:

| Project | Role |
|---------|------|
| **Firefly** | AOT compiler: F# -> PSG -> MLIR -> Native binary |
| **FNCS** | F# Native Compiler Services (type checking) |
| **FSNAC** | F# Native AutoComplete (this repository) |
| **Alloy** | Native standard library with platform bindings |
| **XParsec** | Parser combinators for TOML and PSG traversal |

FSNAC develops in **parallel with FNCS** (FSharpNative Compiler Services). As FNCS matures, FSNAC will consume its enhanced type resolution for native type awareness.

## Supported Editors

FSNAC provides F# support for:
- **Visual Studio Code** (via Ionide configuration)
- **Neovim** (via nvim-lspconfig)
- **Vim** (via vim-fsharp)
- **Emacs** (via emacs-fsharp-mode)
- **Sublime Text** (via LSP package)
- **Helix**, **Kate**, **Zed**

## Architecture

### Core Components

- **FsNativeAutoComplete.Core**: Core functionality including:
  - F# compiler service interfaces
  - Symbol resolution and type checking
  - Project loading (`.fsproj`, `.sln`, AND `.fidproj`)
  - File system abstractions

- **FsNativeAutoComplete**: Main LSP server with:
  - LSP protocol handlers
  - Code fixes and quick actions
  - Program entry point

- **FsNativeAutoComplete.Logging**: Centralized logging

### Key Dependencies

- **FSharp.Compiler.Service**: F# language analysis (future: replaced by FNCS)
- **Ionide.ProjInfo**: Standard project parsing
- **XParsec**: TOML parsing for `.fidproj` files
- **FSharpLint.Core**: Code linting
- **Fantomas.Client**: Code formatting

## Development Phases

### Phase 1: Identity and TOML Support (Current)
- Complete namespace rename to `FsNativeAutoComplete`
- Implement TOML parsing for `.fidproj` via XParsec
- Create `FidprojLoader` producing `FSharpProjectOptions`
- Workspace discovery for `.fidproj` alongside `.fsproj`/`.sln`

### Phase 2: FNCS Integration
- Replace FCS with FNCS for type checking
- Surface native type semantics (string as UTF-8 fat pointer, option as value type) in hover/completion
- Display SRTP witness resolutions

### Phase 3: Metaprogramming Support
- Quotation structure visualization
- Active pattern navigation
- Computation expression builder resolution
- Custom `fidelity/*` LSP endpoints

## Building and Testing

Requirements:
- .NET SDK (see `global.json` for version)

```bash
dotnet tool restore
dotnet build
dotnet test

# Run specific test project
dotnet test -f net8.0 ./test/FsNativeAutoComplete.Tests.Lsp/FsNativeAutoComplete.Tests.Lsp.fsproj
```

## Code Organization

### Project Loading
- `src/FsNativeAutoComplete/LspServers/ProjectWorkspace.fs` - Workspace management
- `src/FsNativeAutoComplete.Core/` - Project options generation
- **TODO**: `src/FsNativeAutoComplete.Core/FidprojLoader.fs` - TOML-based projects

### LSP Endpoints
- `src/FsNativeAutoComplete/LspServers/AdaptiveFSharpLspServer.fs` - Main server
- Custom F#-specific endpoints: `fsharp/*`
- **TODO**: Fidelity-specific endpoints: `fidelity/*`

### Code Fixes
- Located in `src/FsNativeAutoComplete/CodeFixes/`
- Use `dotnet fsi build.fsx -- -p ScaffoldCodeFix YourCodeFixName` to scaffold

## Key Files for FSNAC Features

| File | Purpose |
|------|---------|
| `src/FsNativeAutoComplete.Core/FileSystem.fs` | Add `.fidproj` recognition |
| `src/FsNativeAutoComplete/LspServers/ProjectWorkspace.fs` | Workspace discovery |
| **NEW** `src/FsNativeAutoComplete.Core/FidprojLoader.fs` | TOML parsing |
| `src/FsNativeAutoComplete/LspServers/AdaptiveFSharpLspServer.fs` | Custom endpoints |

## Naming Conventions

- **Namespaces**: `FsNativeAutoComplete.*`
- **CLI tool**: `fsnac`
- **NuGet packages**: `FsNativeAutoComplete`, `FsNativeAutoComplete.Core`
- **Custom LSP methods**: `fidelity/*` for native-specific features

## F# Language Conventions

- Follow F# community conventions
- Use `fantomas` for code formatting
- Prefer immutable data structures
- Use explicit type annotations where they improve clarity
- `CamelCase` for types, `camelCase` for values

## Testing Guidelines

- Tests in `test/FsNativeAutoComplete.Tests.Lsp/`
- **TODO**: Add tests for `.fidproj` loading
- Use descriptive test names
- Include both positive and negative test cases
- Test with realistic F# code examples

## LSP Features

### Standard LSP Endpoints
- `textDocument/completion` with `completionItem/resolve`
- `textDocument/hover`, `definition`, `references`
- `textDocument/codeAction`, `codeLens`
- `textDocument/formatting` (via Fantomas)
- `textDocument/rename`, `signatureHelp`

### Custom F# Endpoints
- `fsharp/signature`, `fsharp/compile`
- `fsharp/workspacePeek`, `fsharp/workspaceLoad`
- `fsproj/addFile`, `fsproj/removeFile`

### Planned Fidelity Endpoints
- `fidelity/projectInfo` - Query `.fidproj` configuration
- `fidelity/memoryLayout` - Type memory layout information
- `fidelity/srtpResolution` - SRTP witness resolution
- `fidelity/platformBindings` - Platform binding mappings

## Resources

### FSNAC-Specific
- [FSNAC README](./README.md)
- [FNCS Repository](https://github.com/speakeztech/fsnative)
- [Fidelity Framework Primer](https://speakez.tech/blog/fidelity-framework-a-primer/)

### Standard References
- [LSP Specification](https://microsoft.github.io/language-server-protocol/)
- [F# Compiler Service Documentation](https://fsharp.github.io/FSharp.Compiler.Service/)
- [F# Style Guide](https://docs.microsoft.com/en-us/dotnet/fsharp/style-guide/)

### Project-Specific
- [Creating a New Code Fix](./docs/Creating%20a%20new%20code%20fix.md)
- [Ionide.ProjInfo Documentation](https://github.com/ionide/proj-info)
- [XParsec Documentation](https://github.com/speakeztech/xparsec)

## Contributing

Areas of particular interest:
1. **TOML parsing integration** - Connecting XParsec TOML parser
2. **FidprojLoader implementation** - Translating `.fidproj` to `FSharpProjectOptions`
3. **Testing with Fidelity projects** - Validating against native F# code
4. **Editor configuration guides** - Documentation for various editors

## Contact

FSNAC is developed by [SpeakEZ Technologies](https://speakez.tech) as part of the Fidelity native compilation framework.

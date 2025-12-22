# FsNativeAutoComplete Project Overview

## Purpose

FsNativeAutoComplete (FSNAC) is a fork of FsAutoComplete (FSAC) modified to support the Fidelity framework ecosystem. It provides LSP (Language Server Protocol) backend services for F# development with two key extensions:

1. **`.fidproj` Support**: Parse TOML-based Fidelity project files and produce `FSharpProjectOptions` for FCS
2. **Improved Serena Integration**: Fix `find_referencing_symbols` and other LSP operations that currently return empty results

## Architecture

### Core Components

- **FsNativeAutoComplete.Core**: Core functionality including:
  - F# compiler service interfaces
  - Code generation and refactoring utilities
  - Symbol resolution and type checking
  - Signature formatting and documentation

- **FsNativeAutoComplete**: Main LSP server implementation with:
  - LSP protocol handlers and endpoints
  - Code fixes and quick actions
  - Program entry point

- **FsNativeAutoComplete.Logging**: Centralized logging infrastructure

### Key Dependencies

- **FSharp.Compiler.Service** (>= 43.9.300): Core F# compiler APIs
- **Ionide.ProjInfo** (>= 0.71.2): Project and solution file parsing
- **FSharpLint.Core**: Code linting and static analysis
- **Fantomas.Client**: F# code formatting
- **Microsoft.Build**: MSBuild integration for project loading

## Development Goals

### Phase 1: `.fidproj` Support
- Implement TOML parser for `.fidproj` files (using XParsec, zero BCL dependencies)
- Create `FidprojLoader` that produces `FSharpProjectOptions`
- Integrate with existing FSAC project loading infrastructure

### Phase 2: Serena LSP Fixes
- Diagnose why `textDocument/references` returns empty results
- Fix project loading to properly index all symbols
- Ensure `find_referencing_symbols` works correctly

## Building

```bash
dotnet tool restore
dotnet build
dotnet test
```

## Key Files

- `src/FsNativeAutoComplete/LspServers/AdaptiveFSharpLspServer.fs` - Main LSP server
- `src/FsNativeAutoComplete.Core/` - Core symbol resolution and analysis
- `FsNativeAutoComplete.sln` - Solution file

## Related Projects

- **Firefly**: AOT F# compiler that will consume FSNAC for IDE support
- **Alloy**: Native F# library used by Fidelity projects
- **XParsec**: Parser combinator library for TOML parsing

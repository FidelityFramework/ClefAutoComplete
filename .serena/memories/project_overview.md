# ClefAutoComplete Project Overview

## Purpose

ClefAutoComplete (formerly FsNativeAutoComplete/FSNAC) is a fork of FsAutoComplete (FSAC) modified to support the Clef language and Fidelity framework ecosystem. It provides LSP (Language Server Protocol) backend services as the LSP backend for Lattice (the Ionide hard-fork). Two key extensions:

1. **`.fidproj` Support**: Parse TOML-based Fidelity project files via CCS's FidprojLoader
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

- **CCS (Clef.Compiler.Service)**: Clef parsing, type checking, PSG construction (BCL-free, forked from FCS)
- **Ionide.ProjInfo** (>= 0.71.2): Project and solution file parsing (bridge period; CCS FidprojLoader replaces for .fidproj)
- **FSharpLint.Core**: Code linting and static analysis
- **Fantomas.Client**: F# code formatting
- **Microsoft.Build**: MSBuild integration for project loading

## Development Goals

### Phase 1: `.fidproj` Support
- Integrate CCS's FidprojLoader (TOML-based, XParsec, zero BCL dependencies)
- Bridge FidprojLoader output to FSharpProjectOptions during bootstrap period
- Integrate with existing FSAC project loading infrastructure

### Phase 2: Serena LSP Fixes
- Diagnose why `textDocument/references` returns empty results
- Fix project loading to properly index all symbols
- Ensure `find_referencing_symbols` works correctly

### Phase 3: CCS Integration (replaces FCS)
- Consume CCS for type checking, hover info, completions, SRTP resolution
- Display native Clef type semantics (no obj, native string, voption)
- Surface PSG diagnostics and depth analysis in editor

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

- **CCS (Clef.Compiler.Service)**: Compiler service (parsing, type checking, PSG) — BCL-free fork of FCS
- **Composer**: AOT compiler (was Firefly) — consumes CCS output → MLIR → native
- **Lattice**: IDE integration (hard-forked Ionide) — consumes ClefAutoComplete as LSP backend
- **Alloy**: Native Clef library (absorbed into compiler core)
- **XParsec**: Parser combinator library used by CCS and ClefAutoComplete

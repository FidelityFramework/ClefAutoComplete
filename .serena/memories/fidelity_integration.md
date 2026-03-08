# Fidelity Integration Roadmap

## Context

ClefAutoComplete (formerly FSNAC/FsNativeAutoComplete) is the IDE companion to CCS (Clef Compiler Services, formerly FNCS). Together they provide a complete development experience for Clef native compilation in the Fidelity framework ecosystem. Lattice (hard-forked from Ionide) is the editor integration layer that consumes ClefAutoComplete as its LSP backend.

## Development Phases

### Phase 1: Project Identity and TOML Support (Current)

**Goal**: Establish ClefAutoComplete as a distinct, usable tool for Fidelity/Clef projects.

| Task | Description | Status |
|------|-------------|--------|
| Namespace rename | Rename from `FsAutoComplete` → `FsNativeAutoComplete` → `ClefAutoComplete` | Done |
| NuGet identity | Publish as distinct packages | Pending |
| TOML parsing | Integrate CCS's FidprojLoader (XParsec-based, zero BCL deps) | Pending |
| FidprojLoader bridge | Produce `FSharpProjectOptions` from FidprojLoader output during bootstrap | Pending |
| Workspace discovery | Recognize `.fidproj` alongside `.fsproj`/`.sln` | Pending |

**Key Files to Modify:**
- `src/FsNativeAutoComplete.Core/FileSystem.fs` - Add `.fidproj` recognition
- `src/FsNativeAutoComplete/LspServers/ProjectWorkspace.fs` - Workspace discovery
- **NEW** `src/FsNativeAutoComplete.Core/FidprojLoader.fs` - TOML parsing

### Phase 2: CCS Integration

**Goal**: Consume CCS (Clef Compiler Services) for native Clef type resolution.

| Feature | Description |
|---------|-------------|
| Native type awareness | Display Clef type semantics (`string` as UTF-8 fat pointer, `option` as value type) in hover info |
| SRTP resolution | Show resolved witness implementations |
| Memory annotations | Surface lifetime and region information |
| Platform binding hints | Indicate platform call resolution |
| PSG diagnostics | Surface depth analysis and saturation diagnostics in editor |

**Coordination with CCS:**
- CCS provides FidprojLoader for project configuration
- ClefAutoComplete consumes CCS type checker (NativeTypedTree) instead of standard FCS
- Hover/completion displays native Clef type info from CCS
- CCS is BCL-free — no obj, no System.Reflection, no IL metadata

### Phase 3: Advanced Metaprogramming Support

**Goal**: Specialized IDE support for Clef's "standing art" features.

#### Quotations as Semantic Carriers
- Quotation structure visualization
- Navigation from quotation to generated code
- Semantic highlighting for compile-time expressions

#### Active Patterns for Structural Recognition
- Pattern composition visualization
- Navigation to pattern definitions from match sites
- Type flow through partial active patterns

#### Computation Expressions as Control Flow
- Builder method resolution
- Continuation structure visualization
- Dialect selection hints (DCont vs Inet)

### Phase 4: Multi-Pane Development

**Goal**: Support coordinated views across compilation stages.

| Pane | Format | Language Server |
|------|--------|-----------------|
| Clef Source | `.clef` / `.fs` | ClefAutoComplete |
| MLIR | `.mlir` | mlir-lsp-server |
| LLVM IR | `.ll` | clangd |

## `.fidproj` Format

Fidelity projects use TOML configuration:

```toml
[package]
name = "my_project"
version = "0.1.0"

[compilation]
memory_model = "stack_only"
target = "native"

[dependencies]
alloy = { path = "../alloy/src" }

[build]
sources = ["Main.fs"]
output = "binary_name"
output_kind = "console"  # or "freestanding"
```

## Custom LSP Endpoints (Planned)

| Endpoint | Purpose |
|----------|---------|
| `fidelity/projectInfo` | Query `.fidproj` configuration |
| `fidelity/memoryLayout` | Get type memory layout information |
| `fidelity/srtpResolution` | Show SRTP witness resolution |
| `fidelity/platformBindings` | List platform binding mappings |

## Integration with Composer

The full IDE experience for Clef/Fidelity development:

```
┌─────────────────────────────────────────────────────────┐
│                     Lattice (Editor)                     │
│        (Hard-forked Ionide for VS Code, nvim, etc.)     │
└─────────────────────────┬───────────────────────────────┘
                          │ LSP
┌─────────────────────────▼───────────────────────────────┐
│                   ClefAutoComplete                       │
│              Clef LSP Server (forked FSAC)              │
├─────────────────────────────────────────────────────────┤
│  FidprojLoader    │   Standard LSP   │   Custom         │
│  (TOML via CCS)   │   Handlers       │   Fidelity API   │
└─────────────────────────┬───────────────────────────────┘
                          │
┌─────────────────────────▼───────────────────────────────┐
│                        CCS                               │
│            Clef Compiler Services (BCL-free)             │
│  (Type checking, SRTP resolution, PSG, Native types)    │
└─────────────────────────┬───────────────────────────────┘
                          │ ClefExpr / PSG
┌─────────────────────────▼───────────────────────────────┐
│                      Composer                            │
│    PSG Saturation → Nanopasses → Alex → MLIR → Native   │
└─────────────────────────────────────────────────────────┘
```

## Self-Hosting Path

The toolchain is designed for progressive decoupling from .NET:

1. **Bootstrap (current)**: All tools run on .NET, compile Clef syntax via CCS+Composer
2. **Bridge**: CCS is already BCL-free. ClefAutoComplete transitions from FCS → CCS.
3. **Self-host**: Composer compiles the tools themselves to native. No .NET runtime needed.

Every C#/.NET interop dependency is a barrier to self-hosting. ClefAutoComplete's fork of FSAC
inherits some C# interop (Ionide.ProjInfo, MSBuild), but these are progressively replaced by
CCS-native equivalents (FidprojLoader, NativeTypedTree).

## Related Documentation

- [Fidelity Framework Primer](https://speakez.tech/blog/fidelity-framework-a-primer/)
- [Standing Art: Clef Metaprogramming in Composer](https://speakez.tech/blog/standing-art-fsharp-metaprogramming-in-firefly/)
- [ClefPak: Native Source-Based Package Management](https://speakez.tech/blog/native-fsharp-source-based-package-mgmt/)

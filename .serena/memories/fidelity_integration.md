# Fidelity Integration Roadmap

## Context

FSNAC (FsNativeAutoComplete) is the IDE companion to FNCS (FSharpNative Compiler Services). Together they provide a complete development experience for native F# compilation in the Fidelity framework ecosystem.

## Development Phases

### Phase 1: Project Identity and TOML Support (Current)

**Goal**: Establish FSNAC as a distinct, usable tool for Fidelity projects.

| Task | Description | Status |
|------|-------------|--------|
| Namespace rename | Complete rename from `FsAutoComplete` to `FsNativeAutoComplete` | Done |
| NuGet identity | Publish as distinct packages | Pending |
| TOML parsing | Integrate XParsec-based parser for `.fidproj` | Pending |
| FidprojLoader | Produce `FSharpProjectOptions` from TOML | Pending |
| Workspace discovery | Recognize `.fidproj` alongside `.fsproj`/`.sln` | Pending |

**Key Files to Modify:**
- `src/FsNativeAutoComplete.Core/FileSystem.fs` - Add `.fidproj` recognition
- `src/FsNativeAutoComplete/LspServers/ProjectWorkspace.fs` - Workspace discovery
- **NEW** `src/FsNativeAutoComplete.Core/FidprojLoader.fs` - TOML parsing

### Phase 2: FNCS Integration

**Goal**: Consume FNCS for enhanced native type resolution.

| Feature | Description |
|---------|-------------|
| Native type awareness | Display native type semantics (`string` as UTF-8 fat pointer, `option` as value type) in hover info |
| SRTP resolution | Show resolved witness implementations |
| Memory annotations | Surface lifetime and region information |
| Platform binding hints | Indicate platform call resolution |

**Coordination with FNCS:**
- FNCS provides `FSharpProjectOptions` with native type configuration
- FSNAC consumes FNCS type checker instead of standard FCS
- Hover/completion displays native type info from FNCS

### Phase 3: Advanced Metaprogramming Support

**Goal**: Specialized IDE support for F#'s "standing art" features.

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
| F# Source | `.fs` | FSNAC |
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

## Integration with Firefly

The full IDE experience for Fidelity development:

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
└─────────────────────────┬───────────────────────────────┘
                          │ Typed Tree
┌─────────────────────────▼───────────────────────────────┐
│                       Firefly                            │
│    PSG Construction → Nanopasses → Alex → MLIR → Native │
└─────────────────────────────────────────────────────────┘
```

## Related Documentation

- [Fidelity Framework Primer](https://speakez.tech/blog/fidelity-framework-a-primer/)
- [Standing Art: F# Metaprogramming in Firefly](https://speakez.tech/blog/standing-art-fsharp-metaprogramming-in-firefly/)
- [Fargo: Native F# Source-Based Package Management](https://speakez.tech/blog/native-fsharp-source-based-package-mgmt/)

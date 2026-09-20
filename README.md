# ClefAutoComplete

A hard fork of [FsAutoComplete](https://github.com/fsharp/FsAutoComplete) at v0.82.0 (2025-12-16), kept as
reference plumbing for the Lattice language server. The local replacement is a thin, .NET-hosted server
in Composer's solution, using the Clef Compiler Service (CCS) for program semantics.

Start with the shared [Lattice integration plan](https://github.com/FidelityFramework/Composer/blob/main/docs/Lattice_Integration.md)
and this repository's [contribution responsibilities](docs/fidelity/README.md). The source server and build
instructions now live in [Composer/src/Lattice.Server](https://github.com/FidelityFramework/Composer/tree/main/src/Lattice.Server).
It has a local VSCode dimensional/proof demo; no server package has been published yet.

**What is here.** The FsAutoComplete body (LSP transport, request routing, code fixes, tests) with its
namespaces renamed, plus a thin bridge (`src/FsNativeAutoComplete.Core/NativeCompilerServiceInterface.fs` and
the `LspServers/Native*` files) written against a compiler that no longer exists: the project reference under
`HAVE_FNCS` points at `~/repos/fsnative`, which holds no compiler, and the bridge opens namespaces that were
renamed when the Clef Compiler Service (`~/repos/clef`, `Clef.Compiler.*`) replaced it. The repository does not
build at HEAD.

**What Lattice is.** The editor tooling of the Fidelity toolchain, and a *witness* of the Program Semantic
Graph the Clef Compiler Service saturates. The intended queries read compiler-owned facts (types with dimensions, residence and layout,
reachability, proof obligations and their verdicts). Lattice computes no independent semantic facts. The queries it answers, the facts
each one reads, and the disposition of this repository are specified in the consumer contract:

- [Lattice consumer contract](https://github.com/FidelityFramework/clef/blob/main/docs/fidelity/phg/Lattice_Consumer_Contract.md)

The contract's position is that the Lattice server is written fresh and thin against CCS (standard LSP
transport plus the contract's query surface, one in-process graph service), and that this fork's body is
retired rather than migrated: FsAutoComplete is an architecture for computing from a typed tree, and only
seven of its ~fifty handlers were ever routed to the graph.

**First contribution.** Use the inherited LSP transport and test arrangements as references for the small
server's protocol harness. CCS owns project source order, unsaved-source checking, position and scope
queries, diagnostics, and the evidence behind them. Lattice translates those results to LSP; VS Code and
Neovim register the language and display the responses. Semantic rules from `lattice-analyzers` need a CCS
diagnostic or query; presentation rules need a separate, explicitly syntactic treatment.

The active server loads ordered `.fidproj` sources and dependencies through CCS, checks unsaved buffers,
and publishes compiler diagnostics, dimensional hovers and resolved definitions for the checked version.
Selected-platform inputs participate in the same project check. `clef/programInitialization` reads the
PSG's startup order, initializer identities and source spans, storage intent and pending native facts;
the editor does not reconstruct execution order. Completion and the wider scope-query surface remain pending.

[VSCode's local development client](https://github.com/FidelityFramework/lattice-vscode/tree/fidelity/client)
registers Clef and provides an F5 launch path without publishing. The [Neovim client](https://github.com/FidelityFramework/lattice-vim)
has its own standard-LSP transport gate. Fixture-server checks exercise client plumbing, not CCS inference.
The replacement server now has a separate real-VSCode gate for dimensional hover, diagnostics, definitions,
unsaved corrections and expandable cvc5 source results. Neovim's semantic gate remains separate work.

**Coordinated implementation.** Composer references the peer `clef` checkout. The
[C-07 implementation record](https://github.com/FidelityFramework/Composer/blob/main/docs/Language_Coverage_Waypoints.md#c-07-sequence-operations--implementation-waypoint-acceptance-open-2026-09-20)
pins the tested compiler/tooling revisions, including 987 CCS tests and the live selected-platform and
source-only startup error/repair gates. Later native corrections retain their separately recorded hashes.
These are bounded results: C-07 acceptance remains open for the original sequence sample and stored/bare
sequence-operation values. This reference fork remains outside the active server's build and does not gain
capabilities from its peers' results.

The [documentation index](docs/README.md) separates reference material from current integration work.
Run `~/repos/clef/docs/fidelity/phg/drift-gate.sh` when validating documentation changes; a vocabulary check
does not replace the compiler and protocol acceptance gates.

Upstream license and attribution: see `LICENSE.md`. FsAutoComplete is the work of the Ionide community.

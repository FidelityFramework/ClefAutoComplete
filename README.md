# ClefAutoComplete

A hard fork of [FsAutoComplete](https://github.com/fsharp/FsAutoComplete) at v0.82.0 (2025-12-16), kept as
reference plumbing for the Lattice language server. It is not the Lattice server, and nothing in it is design.

**What is here.** The FsAutoComplete body (LSP transport, request routing, code fixes, tests) with its
namespaces renamed, plus a thin bridge (`src/FsNativeAutoComplete.Core/NativeCompilerServiceInterface.fs` and
the `LspServers/Native*` files) written against a compiler that no longer exists: the project reference under
`HAVE_FNCS` points at `~/repos/fsnative`, which holds no compiler, and the bridge opens namespaces that were
renamed when the Clef Compiler Service (`~/repos/clef`, `Clef.Compiler.*`) replaced it. The repository does not
build at HEAD.

**What Lattice is.** The editor tooling of the Fidelity toolchain, and a *witness* of the Program Semantic
Graph the Clef Compiler Service saturates: it reads settled facts (types with dimensions, residence and layout,
reachability, proof obligations and their verdicts) and computes nothing. The queries it answers, the facts
each one reads, and the disposition of this repository are specified in the consumer contract:

- `~/repos/clef/docs/fidelity/phg/Lattice_Consumer_Contract.md`

The contract's position is that the Lattice server is written fresh and thin against CCS (standard LSP
transport plus the contract's query surface, one in-process graph service), and that this fork's body is
retired rather than migrated: FsAutoComplete is an architecture for computing from a typed tree, and only
seven of its ~fifty handlers were ever routed to the graph.

**Rule for anyone working here.** Do not extend the FsAutoComplete architecture toward Clef. Do not add
editor-side type inference, symbol resolution, diagnostic policy, or prose keyed on type names. Every fact the
editor shows is read from the graph; if the graph does not carry it, that is a CCS deliverable named in the
contract, not an editor computation. Run `~/repos/clef/docs/fidelity/phg/drift-gate.sh` before proposing a
documentation change.

Upstream license and attribution: see `LICENSE.md`. FsAutoComplete is the work of the Ionide community.

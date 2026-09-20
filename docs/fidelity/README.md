# Lattice integration responsibilities

The [shared implementation plan](https://github.com/FidelityFramework/Composer/blob/main/docs/Lattice_Integration.md)
coordinates CCS, Composer, the editor clients, and the analyzer reference corpus. This page identifies the
useful work associated with ClefAutoComplete. It does not define a second server architecture.

## Current reference code

The fork contains LSP lifecycle handling, JSON-RPC plumbing, feature routing, and a substantial test harness.
Its [core project](../../src/FsNativeAutoComplete.Core/FsNativeAutoComplete.Core.fsproj#L12) references an
obsolete compiler project. Its native document path also retains a separate FCS path:
[open handling](../../src/FsNativeAutoComplete/LspServers/AdaptiveFSharpLspServer.fs#L625) invokes both,
and [change handling](../../src/FsNativeAutoComplete/LspServers/AdaptiveFSharpLspServer.fs#L697) leaves native
incremental edits unsupported. These are reference arrangements to inspect, not a working CCS integration.

The [LSP test harness](../../test/FsNativeAutoComplete.Tests.Lsp/Utils/Server.Tests.fs) is a useful starting
point for lifecycle and request/response cases. Semantic assertions for the new server must compare with
the selected CCS snapshot, rather than inherit this fork's name-based definition search or completion policy.

## Responsibilities and implemented boundary

| Owner | Responsibility |
|---|---|
| CCS | Ordered `.fidproj` sources; checking with unsaved buffers; structured diagnostics; source-position and scope queries over its graph; retained evidence and dependencies |
| Composer / Lattice server | A small .NET-hosted LSP project using the aligned CCS build, with one workspace checking service, versioned responses, and scheduling that prevents stale results from replacing newer ones |
| This reference repository | Identify reusable transport patterns and protocol test cases; keep the status and source pointers accurate |
| VS Code / Neovim clients | Registration, transport startup, document synchronization, and display of the capabilities the new server actually advertises |
| Analyzer migration | Classify each useful rule as a compiler diagnostic, a query over compiler facts, or presentation; retain the source cases as regression inputs |

Composer's [CCS.Editor](https://github.com/FidelityFramework/Composer/tree/main/src/CCS.Editor)
calls `ProjectChecker.checkProjectWithVolatile` and freezes revision-associated source views. The active
server projects hover, resolved definitions and located compiler diagnostics, observes project/dependency
inputs, and prevents superseded checks from replacing newer results. Proof request cancellation and
edit invalidation have separate transport gates. Structured parser diagnostic ranges, completion,
references, semantic tokens and multiple-project service remain outside the advertised boundary.

Startup is design-time graph information as well as executable structure. The immutable editor snapshot
and `clef/programInitialization` read the compiler's ordered initializer IDs and spans, storage intent,
settled space authority and pending dependency facts. A source-only check can expose intent and a native
settlement requirement without pretending that physical storage is admitted. No editor-side analysis
selects initializers or supplies their order.

CCS supplies the semantics even when the server is written in F# and runs on .NET. Host names alone do not
identify a fallback. The acceptance test must establish which checker handled the document and which source
version produced each response.

## Recorded acceptance gates

### Planned target-aware projections — 2026-09-20

[Composer M-01 §5](../../../Composer/docs/PRDs/M-01-DialectAdmission.md#5-numeric-selection-parallelism-and-design-time-projection)
adds numeric-selection, arithmetic-construction and concurrency capability cases
to the shared plan. The active CCS/Lattice service must expose the selected
profile, graph-owned eligibility and source-related evidence, distinguishing
representation error, computation error, reproducibility and progress premises.
Target/declaration changes must invalidate dependent responses. Preserve exact
compiler diagnostics through unsaved edits and repair, including rejected numeric
capabilities and unavailable timeout recovery. These are planned acceptance
cases, not new handlers in this reference fork. No independent numeric solver,
wait-graph analysis or migration back to the inherited server is authorized by
this plan. The [coordinated waypoints](../../../Composer/docs/Language_Coverage_Waypoints.md)
retain implementation status and revisions.

### Existing evidence

The current [coverage record](https://github.com/FidelityFramework/Composer/blob/main/docs/Language_Coverage_Waypoints.md)
pins the actual artifacts and evidence. Active checks cover:

1. Initialize and shut down through standard LSP; advertise only supported requests and synchronization.
2. Open unsaved text and obtain CCS diagnostics with the correct file and source range.
3. Request a dimensional hover and a definition, comparing both with the same checked compiler result.
4. Introduce and repair an error through document changes; verify the diagnostics update and clear, including
   changes affecting the other source file. An older check must not overwrite the newer response.
5. Rename selected-platform storage designations and check exact CCS8206/CCS8207 errors, cross-file
   definitions and repairs. Query source-only startup order and an opaque initializer's pending fact.

The C-07 consolidated compiler gate passed 987 tests; the linked editor projection and both live startup
and platform LSP gates passed on `da1f5790`. Subsequent native changes have their own recorded artifacts.
The real VSCode semantic/proof host gate is separate from these stdio gates. Neovim currently has a
protocol-fixture gate; its CCS semantic gate remains pending. C-07 language acceptance is still open,
and no server package has been published.

Proof presentation follows the evidence CCS actually supplies. The active server dispatches
compiler-authored source queries to cvc5 and distinguishes proved, counterexample, unknown and error
results. An obligation or emitted query alone is not a verdict, and source discharge does not certify
native lowering. Scope completion and richer graph views need their own compiler query contracts.

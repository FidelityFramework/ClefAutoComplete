# Option tooling waypoint — 2026-09-19

The Option work is checked through the same CCS results consumed by Lattice. This
repository remains reference material for transport and tests; its retired bridge
does not receive another completion or intrinsic implementation. The governing
[consumer contract](https://github.com/FidelityFramework/clef/blob/main/docs/fidelity/phg/Lattice_Consumer_Contract.md)
and [integration plan](https://github.com/FidelityFramework/Composer/blob/main/docs/Lattice_Integration.md)
continue to assign semantic authority to CCS.

## Compiler and consumer handoff

| Layer | Waypoint responsibility |
|---|---|
| [Clef specification](https://github.com/FidelityFramework/clef-lang-spec) | The admitted language contract, including Option fallback evaluation and dimensional payload identity |
| [CCS source resolution](https://github.com/FidelityFramework/clef/blob/main/src/Compiler/NativeTypedTree/Expressions/Intrinsics.fs) | Fresh source-level type schemes and lexical resolution; the editor must not duplicate an intrinsic catalogue |
| [CCS service tests](https://github.com/FidelityFramework/clef/tree/main/tests/Clef.Compiler.Service.Tests) | Admitted source, graph structure, and rejected source with effective diagnostic severity, code and exact source span |
| [Composer](https://github.com/FidelityFramework/Composer) | Baker/Alex integration and the native FidelityHello oracles; editor checks do not replace these gates |
| [CCS.Editor](https://github.com/FidelityFramework/Composer/tree/main/src/CCS.Editor) | Immutable, revision-associated hover and diagnostic views of the selected CCS result |
| [Lattice.Server](https://github.com/FidelityFramework/Composer/tree/main/src/Lattice.Server) | LSP transport of those views and publication for the corresponding unsaved document version |
| [VS Code client](https://github.com/FidelityFramework/lattice-vscode/tree/fidelity/client) | Standard language-client presentation; the focused Option gate below uses the real server |
| [Neovim client](https://github.com/FidelityFramework/lattice-vim) | Standard LSP registration and transport; its existing fixture gate is separate from a CCS semantic gate |
| [Analyzer corpus](https://github.com/FidelityFramework/lattice-analyzers) | Compiler-owned analysis prerequisites and source regressions; its [CCS.Editor gate](https://github.com/FidelityFramework/lattice-analyzers/tree/main/tests/Lattice.CCS.Integration) checks the immutable projection; required admission checks remain on the compiler path |
| [Grammar](https://github.com/FidelityFramework/clef-grammar) | First-paint lexical presentation; no Option semantics |
| [VS Code helpers](https://github.com/FidelityFramework/lattice-vscode-helpers) | Retained host plumbing for the historical Fable client; the current thin JavaScript client does not depend on this library |

`Option.defaultValue` uses an eager fallback value. This waypoint adds
`Option.defaultWith`, admitting a `unit -> 'a` fallback producer and preserving the payload's
dimensional type. Compiler and native tests establish evaluation behavior; a
hover or an absence of editor errors does not establish when a producer executes.

The following increment adds `Option.orElse` and `Option.orElseWith`, retaining
the optional result: an eager optional fallback or a deferred `unit -> 'a option`
producer. The same compiler projection gate covers direct, partial and bare-alias
uses, dimensional results and precise rejected applications. There is no new
client-side intrinsic catalogue.

`Option.iter` adds unit-returning actions, including stored partials and bare
aliases at independent dimensional payloads. Its editor gate checks the unit
result, measured partial signature and exact rejected callback/argument spans;
Composer's native gate separately checks eager operands and Some-only invocation.

The native `Option.fold` and `Option.foldBack` contracts quantify state and
payload independently, including their dimensions. Their partial signatures
therefore differ: `fold folder state` awaits an option, while
`foldBack folder option` awaits state. The active gates project those signatures
and compiler diagnostics; callback order and operand timing remain native gates.

Direct capture elaboration also retains two distinct views: the semantic callable
includes its hidden capture formals, while CCS.Editor projects the source signature
and follows compiler-owned capture provenance for definition navigation. Numeric
read ranges belong to the checked observation and revision; saved predicates about
mutable values do not justify reusing those bounds after later writes.

Integer loop variables have an immutable source binding for each iteration.
Their hovers and definitions refer to that identifier; an internal mutable
counter belongs only to elaboration. Capture provenance must retain the source
binding, including nested loops that reuse a name. The peered gate checks this
view and the compiler's rejection of assignments to the iteration variable.

The same handoff now covers `Result.map`, `Result.mapError` and `Result.bind`.
Success and error payloads quantify independently; changing one case preserves
the other case's payload type and dimensions. The projection gates check both
measured result parameters, stored callback signatures, explicit type arguments
and exact rejected applications. Native callback and payload preservation remain
separate Composer gates, recorded with their status in the
[coverage waypoint](https://github.com/FidelityFramework/Composer/blob/main/docs/Language_Coverage_Waypoints.md).

`Result.defaultValue`, `Result.defaultWith` and `Result.iter` use that same
projection. Recovery handlers receive the typed Error payload; actions receive
the typed Ok payload and return unit. The editor preserves both parameters in a
stored partial signature and reports mismatched callback dimensions at the
compiler's source span. A function-valued default retains its ordinary callable
result; native gates establish its evaluation boundary.

`Result.isOk` and `Result.isError` retain both independent payload types in a
stored predicate's signature and return bool. Their tag-only Baker elaboration
does not extract or invoke a payload. CCS owns both dimensional mismatch and
extra-application diagnostics; peered editor checks retain those exact ranges
and restore bool hovers after repair.

## Editor gate

The live client contains
[`test/server-options.cjs`](https://github.com/FidelityFramework/lattice-vscode/blob/fidelity/client/test/server-options.cjs).
It starts the explicitly selected Lattice assembly over stdio and checks measured
Option result and partial-application hover. Unsaved rejected applications must
retain CCS's diagnostic code, effective error severity, and complete source range.
Correcting each edit must clear the error at the new document version and restore
the measured hover. The gate records all server assembly hashes and diagnostic
publications in a temporary evidence directory; it does not build the compiler.

After the coordinated compiler/server build, run with Node.js 22 or later:

```sh
cd ../lattice-vscode/client
npm ci
npm test
npm run test:options
```

`LATTICE_SERVER_DLL`, `LATTICE_COMPOSER_ROOT` and `LATTICE_DOTNET` select an explicit
local server and host. The Option gate needs no platform library, native build,
solver result, or VS Code installation. The separate `test:ccs-host` gate covers
the real editor and proof presentation; a successful stdio gate alone does not
claim that rendered-editor coverage.

## Completion remains a compiler query

Computation syntax must retain the compiler's admission result. Unsupported
builder, bind, return or resource-use forms must not appear as ordinary accepted
applications after their meaning is erased. The peered diagnostic gate checks
located CCS8401 errors and repair through admitted ordinary FP expressions.
Existing native sequence admission is a separate source gate; it does not imply
general builder dispatch or completed sequence frame/lifetime implementation.

Sequence elaboration now creates its graph owner before checking the body. All
owned `yield` values constrain one element type; `yield!` constrains the delegated
sequence to that same type. Nested sequences have independent owners. Tooling
projects the resulting dimensional/type diagnostics and repaired element hovers
from CCS, including annotation conflicts. This source constraint checkpoint does
not establish native sequence frame/formal settlement or execution conformance.

The generator's internal formal now has its own typed graph identity and a
zero-width source anchor, with provenance through its generator and sequence
owner. It occupies no source token. The peered projection checks the source
sequence type and navigation from a captured value to its original declaration;
the internal formal must not replace either. Frame representation and suspension
semantics remain separate work.

The `Seq.map`, `filter`, `collect` and `append` producer recipes now preserve
formation-time operand snapshots and generator-local capture references.
Generated bindings occupy source points; the replacement expression retains the
full source call range. Peered checks require `seq<T>` application-result hovers,
navigation to original captured declarations, and exact dimensional rejections
with repair. Snapshot construction is compiler-owned and adds no editor lookup
or completion catalogue. These gates do not establish native suspension frames.

Suspension ownership adds a compiler-owned delimiter hyperedge relating each
yield/delegation site to its sequence and generator. CCS tests assert the raw
incidence after Baker elaboration; editor gates observe source types and
definitions for nested owners, guarded effects, delegation and effectful bodies
without a yield. Yields in ordinary lambda/lazy bodies retain CCS8401 rejection
and repair. This checkpoint adds no frame layout, resumption state, native
execution claim or editor graph API; malformed internal ownership diagnostics
remain compiler admission checks.

Owned `yield!` now elaborates through Baker's shared sequence ingredient into a
generator-local enumerator binding and explicit move/current/yield loop. Its
source node remains a unit-valued sequential wrapper with the original range and
operand; ownership is reapplied to the generated yield. Peered source projections
cover an effectful operand and nested append/collect, preserving dimensional
hovers and captured definitions. Guarded execution, exhaustion, evaluation
splitting and native sequence behavior remain separate gates.

Local `SequenceEvaluation` hyperedges record compiler-owned evaluation
relationships. CCS tests cover their incidence, guarded branches, loop backedges
and deferred-body formation boundaries. Peered source cases retain measured
sequence results, lambda signatures and original capture definitions across
guarded-yield loops and captured lambda/lazy values. Lattice observes those
public projections; this checkpoint adds no client evaluation model or native
suspension-execution claim.

The current `CCS.Editor` surface exposes snapshots, hover and definitions, but no
scope/completion query. `Lattice.Server` therefore does not advertise completion.
Adding a client-side list of Option names would create a second semantic authority
and would ignore lexical shadowing, partial applications and the selected compiler
snapshot.

The completion waypoint must expose visible bindings and admitted intrinsic
members through a compiler-owned query, retaining their instantiated types,
dimensions, source identity and snapshot revision. Lattice can then translate
those returned facts to standard LSP completion items. Its acceptance cases must
cover module/member visibility, shadowing, unsaved edits and stale responses.
Until that query and its gate land, Option compiler support does not imply Option
completion support.

Record the selected specification, CCS, Composer and consumer commits together at
each waypoint. Keep the native source oracle, compiler negative cases, editor
projection and any analyzer prerequisite changes in that same review record.

# `obj` Elimination: Why ClefAutoComplete Must Be Parallel

## Core Architectural Decision

Clef eliminates `obj` (System.Object) from the type universe entirely. This is documented in `clef-lang-spec` and has profound implications for ClefAutoComplete.

## Why ClefAutoComplete Cannot Extend FSAC

FSAC uses `obj` pervasively in its internals:

| FSAC Usage | Purpose | ClefAutoComplete Alternative |
|------------|---------|-------------------|
| `obj` as value container | Store typed values uniformly | Type-specific handling |
| FSI result storage | Hold evaluation results | SRTP-based formatters |
| `%A` formatting | Reflection-based display | Compile-time SRTP |
| Hover info boxing | Universal value display | Type-aware formatters |

**There is no `obj` escape hatch.** Every piece of FSAC code that touches values at runtime assumes `obj` exists.

## Parallel Toolchain Model

```
.fsproj → FSAC (uses FCS, has obj)
.fidproj → ClefAutoComplete (uses CCS, no obj)
```

Same IDE (Lattice, hard-forked from Ionide), different backends. Routes based on project type.

## Value Display Without `obj`

ClefAutoComplete must generate SRTP-based formatters at compile time:

```fsharp
// Instead of: sprintf "%A" (value :> obj)
// FSNAC generates: Displayable $ value

type Displayable = Displayable
    with static member inline ($) (Displayable, x: int) = intToString x
         static member inline ($) (Displayable, x: string) = "\"" + x + "\""  // string has native semantics
         // ... more overloads
```

Each hover info request requires:
1. Type-check the expression
2. Generate display formatter via SRTP
3. Compile if needed
4. Format result

## Hover Info Differences

| FSAC | ClefAutoComplete |
|------|------------------|
| `val x : string` (BCL) | `val x : string` (native UTF-8 fat pointer) |
| `val opt : int option` | `val opt : int voption` |
| Shows BCL types | Shows native Clef types |
| Can show any value via obj | Must have SRTP formatter |

## Implementation Notes

When implementing features, remember:
- No `box`/`unbox` operations
- No `obj` parameter types
- No reflection-based dispatch
- All polymorphism via SRTP

## Cross-References

- `clef-lang-spec` memory: `obj_elimination`
- `clef-lang-spec` memory: `parallel_toolchain_architecture`
- `Composer` memory: `ccs_architecture`
- `clef-lang-spec` chapter: `spec/interactive-development.md`
- `clef-lang-spec` chapter: `spec/native-type-mappings.md`

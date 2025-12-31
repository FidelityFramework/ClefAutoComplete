# `obj` Elimination: Why FSNAC Must Be Parallel

## Core Architectural Decision

F# Native eliminates `obj` (System.Object) from the type universe entirely. This is documented in `fsnative-spec` and has profound implications for FSNAC.

## Why FSNAC Cannot Extend FSAC

FSAC uses `obj` pervasively in its internals:

| FSAC Usage | Purpose | FSNAC Alternative |
|------------|---------|-------------------|
| `obj` as value container | Store typed values uniformly | Type-specific handling |
| FSI result storage | Hold evaluation results | SRTP-based formatters |
| `%A` formatting | Reflection-based display | Compile-time SRTP |
| Hover info boxing | Universal value display | Type-aware formatters |

**There is no `obj` escape hatch.** Every piece of FSAC code that touches values at runtime assumes `obj` exists.

## Parallel Toolchain Model

```
.fsproj → FSAC (uses FCS, has obj)
.fidproj → FSNAC (uses FNCS, no obj)
```

Same IDE, different backends. Ionide routes based on project type.

## Value Display Without `obj`

FSNAC must generate SRTP-based formatters at compile time:

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

| FSAC | FSNAC |
|------|-------|
| `val x : string` (BCL) | `val x : string` (native UTF-8 fat pointer) |
| `val opt : int option` | `val opt : int voption` |
| Shows BCL types | Shows native types |
| Can show any value via obj | Must have SRTP formatter |

## Implementation Notes

When implementing features, remember:
- No `box`/`unbox` operations
- No `obj` parameter types
- No reflection-based dispatch
- All polymorphism via SRTP

## Cross-References

- `fsnative-spec` memory: `obj_elimination`
- `fsnative-spec` memory: `parallel_toolchain_architecture`
- `Firefly` memory: `fncs_architecture`
- `fsnative-spec` chapter: `spec/interactive-development.md`
- `fsnative-spec` chapter: `spec/native-type-mappings.md`

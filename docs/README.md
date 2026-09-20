# ClefAutoComplete documentation

This repository preserves FsAutoComplete transport, handler, and test code as reference material. The
active Lattice server is .NET-hosted in Composer's solution and reads CCS results through a thin LSP layer.

- [Shared Lattice integration plan](https://github.com/FidelityFramework/Composer/blob/main/docs/Lattice_Integration.md): compiler alignment, responsibilities, and acceptance gates across repositories.
- [Contribution responsibilities](fidelity/README.md): retained reference code, implemented projections and remaining boundaries.
- [Coordinated compiler/tooling evidence](https://github.com/FidelityFramework/Composer/blob/main/docs/Language_Coverage_Waypoints.md): exact revisions and source, native and editor gates; C-07 acceptance remains open.
- [Repository status](../README.md): the obsolete compiler reference and current build limitation.
- [Creating a new code fix](<Creating a new code fix.md>): inherited FsAutoComplete guidance for understanding its implementation. Clef semantic fixes require compiler-owned facts and justification.

The [Clef specification](https://github.com/FidelityFramework/clef-lang-spec) governs semantics. The
[Lattice consumer contract](https://github.com/FidelityFramework/clef/blob/main/docs/fidelity/phg/Lattice_Consumer_Contract.md)
records compiler and consumer responsibilities. Its dated inventory is historical;
the implementation record tracks which portions of that contract the current server exposes.

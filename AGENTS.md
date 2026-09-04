# Agent instructions

This repository is a FsAutoComplete fork kept as reference plumbing. Read `README.md` and the consumer
contract at `~/repos/clef/docs/fidelity/phg/Lattice_Consumer_Contract.md` before touching anything.

- The Program Semantic Graph produced by the Clef Compiler Service (`~/repos/clef`) is the sole semantic
  authority. The editor witnesses it and computes nothing.
- Do not route more FsAutoComplete handlers to the graph; the contract retires this body in favour of a thin
  server written against CCS. Work on the new server happens where CCS builds (Composer's solution), not here.
- Do not mint diagnostics, codes, hover prose, platform-binding lists, or layouts in the editor. Codes are
  `CCS8xxx` from the checker and the obligation ledger; representation facts are node annotations.
- Names: the language is Clef, the service is CCS, the compiler is Composer, the server is Lattice.
  FNCS, F# Native, fsnative, FSNAC and Firefly are retired vocabulary and fail the drift gate.
- The compiler this repository references under `HAVE_FNCS` does not exist; do not "fix" the reference by
  re-pointing the bridge at CCS. The bridge is the thing being replaced.

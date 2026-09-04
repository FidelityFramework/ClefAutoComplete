# Changelog

All notable changes to FsNativeAutoComplete (FSNAC) will be documented in this file.

This project is a fork of [FsAutoComplete](https://github.com/ionide/FsAutoComplete) enhanced for native F# compilation with FNCS (F# Native Compiler Services).

For pre-fork history, see [upstream CHANGELOG](https://github.com/ionide/FsAutoComplete/blob/main/CHANGELOG.md).

## [0.1.0] - 2026-01-01

### Added

- Native project support for `.fidproj` (TOML format) project files
- Native script support for `.fsnx` script files with FSNI directives
- FNCS integration (F# Native Compiler Services) for type checking
- Native type display showing UTF-8 strings, voption, platform words
- CCS8xxx diagnostics for native-specific error codes (from the Clef Compiler Service; none minted here)
- Dual-mode routing for automatic detection of native vs standard projects
- Platform binding detection showing Platform.Bindings module info

[<AutoOpen>]
module private FsNativeAutoComplete.Tests.CodeFixTests.Utils

open Ionide.LanguageServerProtocol.Types
open FsNativeAutoComplete.Logging
open FsToolkit.ErrorHandling
open FsNativeAutoComplete.LspHelpers

module Diagnostics =
  let expectCode code (diags: Diagnostic[]) =
    let diagMsgs =
      diags
      |> Array.choose (fun d -> Option.zip d.CodeAsString (Some d.Message))
      |> Array.map (fun (code, msg) -> $"{code}: {msg}")
      |> String.concat ", "

    Expecto.Flip.Expect.exists
      $"There should be a Diagnostic with code %s{code} but were: {diagMsgs} "
      (fun (d: Diagnostic) ->
        d.CodeAsString = Some code)
      diags

  let acceptAll = ignore

  let private logger =
    FsNativeAutoComplete.Logging.LogProvider.getLoggerByName "CodeFixes.Diagnostics"

  /// Usage: `(Diagnostics.log >> Diagnostics.expectCode "XXX")`
  /// Logs as `info`
  let log (diags: Diagnostic[]) =
    logger.info (
      Log.setMessage "diags({count})={diags}"
      >> Log.addContext "count" diags.Length
      >> Log.addContextDestructured "diags" diags
    )

    diags

module CodeFix =
  let private logger =
    FsNativeAutoComplete.Logging.LogProvider.getLoggerByName "CodeFixes.CodeFix"

  /// Usage: `(CodeFix.log >> CodeFix.withTitle "XXX")`
  /// Logs as `info`
  let log (codeActions: CodeAction[]) =
    logger.info (
      Log.setMessage "codeActions({count})={codeActions}"
      >> Log.addContext "count" codeActions.Length
      >> Log.addContextDestructured "codeActions" codeActions
    )

    codeActions

/// `ignore testCaseAsync`
///
/// Like `testCaseAsync`, but test gets completely ignored.
/// Unlike `ptestCaseAsync` (pending), this here doesn't even show up in Expecto summary.
///
/// -> Used to mark issues & shortcomings in CodeFixes, but without any (immediate) intention to fix
///    (vs. `pending` -> marked for fixing)
/// -> ~ uncommenting tests without actual uncommenting
let itestCaseAsync _name _test = ()

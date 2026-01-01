/// Test source file for native LSP validation
module HelloNative.Main

open Alloy.Core
open Alloy.Console

/// A simple greeting function
let greet (name: string) : string =
    "Hello, " + name + "!"

/// Entry point
let main () =
    let message = greet "Native F#"
    WriteLine message
    0

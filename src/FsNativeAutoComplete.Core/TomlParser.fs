/// Simple TOML Parser for .fidproj files
/// Supports the subset of TOML needed for Fidelity project configuration.
/// Self-contained - no external parser dependencies.
module FsNativeAutoComplete.Core.TomlParser

open System
open System.Text

// =============================================================================
// TOML Value Types
// =============================================================================

type TomlValue =
    | TomlString of string
    | TomlInt of int64
    | TomlFloat of float
    | TomlBool of bool
    | TomlArray of TomlValue list
    | TomlTable of Map<string, TomlValue>
    | TomlInlineTable of Map<string, TomlValue>

type TomlDocument = Map<string, TomlValue>

// =============================================================================
// Parser State
// =============================================================================

type private ParserState = {
    Input: string
    mutable Position: int
}

module private ParserState =
    let create input = { Input = input; Position = 0 }
    let atEnd state = state.Position >= state.Input.Length
    let peek state = if atEnd state then None else Some state.Input.[state.Position]
    let advance state = state.Position <- state.Position + 1
    let current state = state.Input.[state.Position]

// =============================================================================
// Character Classification
// =============================================================================

let private isWhitespace c = c = ' ' || c = '\t'
let private isNewline c = c = '\n' || c = '\r'
let private isDigit c = c >= '0' && c <= '9'
let private isBareKeyChar c =
    (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') ||
    (c >= '0' && c <= '9') || c = '_' || c = '-'

// =============================================================================
// Basic Parsing Helpers
// =============================================================================

let private skipWhitespace state =
    while not (ParserState.atEnd state) && isWhitespace (ParserState.current state) do
        ParserState.advance state

let private skipWhitespaceAndNewlines state =
    while not (ParserState.atEnd state) &&
          (isWhitespace (ParserState.current state) || isNewline (ParserState.current state)) do
        ParserState.advance state

let private skipToEndOfLine state =
    while not (ParserState.atEnd state) && not (isNewline (ParserState.current state)) do
        ParserState.advance state
    if not (ParserState.atEnd state) then
        ParserState.advance state // Skip the newline

let private skipComment state =
    if not (ParserState.atEnd state) && ParserState.current state = '#' then
        skipToEndOfLine state

let private skipBlankLinesAndComments state =
    let mutable cont = true
    while cont && not (ParserState.atEnd state) do
        skipWhitespace state
        match ParserState.peek state with
        | Some '#' -> skipComment state
        | Some c when isNewline c -> ParserState.advance state
        | _ -> cont <- false

let private expect (c: char) state =
    skipWhitespace state
    if ParserState.atEnd state then
        Error $"Expected '{c}' but reached end of input"
    elif ParserState.current state = c then
        ParserState.advance state
        Ok ()
    else
        Error $"Expected '{c}' but got '{ParserState.current state}'"

// =============================================================================
// String Parsing
// =============================================================================

let private parseQuotedString state =
    match expect '"' state with
    | Error e -> Error e
    | Ok () ->
        let sb = StringBuilder()
        let mutable error = None
        let mutable inEscape = false

        while error.IsNone && not (ParserState.atEnd state) do
            let c = ParserState.current state
            if inEscape then
                match c with
                | 'n' -> sb.Append('\n') |> ignore
                | 't' -> sb.Append('\t') |> ignore
                | 'r' -> sb.Append('\r') |> ignore
                | '\\' -> sb.Append('\\') |> ignore
                | '"' -> sb.Append('"') |> ignore
                | _ -> error <- Some $"Invalid escape sequence: \\{c}"
                inEscape <- false
                ParserState.advance state
            elif c = '\\' then
                inEscape <- true
                ParserState.advance state
            elif c = '"' then
                ParserState.advance state
                error <- None  // Signal completion
                // Exit loop handled below
            elif isNewline c then
                error <- Some "Unexpected newline in string"
            else
                sb.Append(c) |> ignore
                ParserState.advance state

        // Check if we exited because we found the closing quote
        if error.IsNone && state.Position > 0 && state.Input.[state.Position - 1] = '"' then
            Ok (sb.ToString())
        elif error.IsSome then
            Error error.Value
        else
            Error "Unclosed string"

let private parseBareKey state =
    let sb = StringBuilder()
    while not (ParserState.atEnd state) && isBareKeyChar (ParserState.current state) do
        sb.Append(ParserState.current state) |> ignore
        ParserState.advance state
    if sb.Length = 0 then
        Error "Expected key"
    else
        Ok (sb.ToString())

let private parseKey state =
    skipWhitespace state
    if ParserState.atEnd state then
        Error "Expected key but reached end of input"
    elif ParserState.current state = '"' then
        parseQuotedString state
    else
        parseBareKey state

// =============================================================================
// Value Parsing
// =============================================================================

let rec private parseValue state =
    skipWhitespace state
    if ParserState.atEnd state then
        Error "Expected value but reached end of input"
    else
        let c = ParserState.current state
        if c = '"' then
            parseQuotedString state |> Result.map TomlString
        elif c = '[' then
            parseArray state
        elif c = '{' then
            parseInlineTable state
        elif c = 't' || c = 'f' then
            parseBool state
        elif c = '-' || c = '+' || isDigit c then
            parseNumber state
        else
            Error $"Unexpected character: '{c}'"

and private parseArray state =
    match expect '[' state with
    | Error e -> Error e
    | Ok () ->
        skipWhitespaceAndNewlines state
        let mutable items = []
        let mutable error = None
        let mutable first = true

        while error.IsNone && not (ParserState.atEnd state) && ParserState.current state <> ']' do
            if not first then
                skipWhitespaceAndNewlines state
                if not (ParserState.atEnd state) && ParserState.current state = ',' then
                    ParserState.advance state
                    skipWhitespaceAndNewlines state
                elif not (ParserState.atEnd state) && ParserState.current state <> ']' then
                    error <- Some "Expected ',' or ']' in array"

            if error.IsNone && not (ParserState.atEnd state) && ParserState.current state <> ']' then
                match parseValue state with
                | Ok v -> items <- items @ [v]
                | Error e -> error <- Some e

            first <- false
            skipWhitespaceAndNewlines state

        match error with
        | Some e -> Error e
        | None ->
            match expect ']' state with
            | Error e -> Error e
            | Ok () -> Ok (TomlArray items)

and private parseInlineTable state =
    match expect '{' state with
    | Error e -> Error e
    | Ok () ->
        skipWhitespace state
        let mutable entries = Map.empty
        let mutable error = None
        let mutable first = true

        while error.IsNone && not (ParserState.atEnd state) && ParserState.current state <> '}' do
            if not first then
                skipWhitespace state
                if not (ParserState.atEnd state) && ParserState.current state = ',' then
                    ParserState.advance state
                    skipWhitespace state
                elif not (ParserState.atEnd state) && ParserState.current state <> '}' then
                    error <- Some "Expected ',' or '}' in inline table"

            if error.IsNone && not (ParserState.atEnd state) && ParserState.current state <> '}' then
                match parseKey state with
                | Error e -> error <- Some e
                | Ok key ->
                    match expect '=' state with
                    | Error e -> error <- Some e
                    | Ok () ->
                        match parseValue state with
                        | Error e -> error <- Some e
                        | Ok value -> entries <- Map.add key value entries

            first <- false
            skipWhitespace state

        match error with
        | Some e -> Error e
        | None ->
            match expect '}' state with
            | Error e -> Error e
            | Ok () -> Ok (TomlInlineTable entries)

and private parseBool state =
    let remaining = state.Input.Substring(state.Position)
    if remaining.StartsWith("true") then
        state.Position <- state.Position + 4
        Ok (TomlBool true)
    elif remaining.StartsWith("false") then
        state.Position <- state.Position + 5
        Ok (TomlBool false)
    else
        Error "Expected 'true' or 'false'"

and private parseNumber state =
    let startPos = state.Position
    let mutable hasDecimal = false

    // Handle sign
    if not (ParserState.atEnd state) && (ParserState.current state = '-' || ParserState.current state = '+') then
        ParserState.advance state

    // Parse digits
    while not (ParserState.atEnd state) &&
          (isDigit (ParserState.current state) || ParserState.current state = '.') do
        if ParserState.current state = '.' then
            hasDecimal <- true
        ParserState.advance state

    let numStr = state.Input.Substring(startPos, state.Position - startPos)
    if hasDecimal then
        match Double.TryParse(numStr) with
        | true, f -> Ok (TomlFloat f)
        | false, _ -> Error $"Invalid float: {numStr}"
    else
        match Int64.TryParse(numStr) with
        | true, i -> Ok (TomlInt i)
        | false, _ -> Error $"Invalid integer: {numStr}"

// =============================================================================
// Document Parsing
// =============================================================================

let private parseSectionHeader state =
    match expect '[' state with
    | Error e -> Error e
    | Ok () ->
        let parts = ResizeArray<string>()
        let mutable error = None
        let mutable first = true

        while error.IsNone && not (ParserState.atEnd state) && ParserState.current state <> ']' do
            if not first then
                if ParserState.current state = '.' then
                    ParserState.advance state
                else
                    error <- Some "Expected '.' or ']' in section header"

            if error.IsNone then
                match parseBareKey state with
                | Ok key -> parts.Add(key)
                | Error e -> error <- Some e

            first <- false

        match error with
        | Some e -> Error e
        | None ->
            match expect ']' state with
            | Error e -> Error e
            | Ok () -> Ok (String.Join(".", parts))

let private parseKeyValue state =
    match parseKey state with
    | Error e -> Error e
    | Ok key ->
        match expect '=' state with
        | Error e -> Error e
        | Ok () ->
            match parseValue state with
            | Error e -> Error e
            | Ok value -> Ok (key, value)

// =============================================================================
// Public API
// =============================================================================

/// Parse a TOML string and return the document
let parse (input: string) : Result<TomlDocument, string> =
    let state = ParserState.create input
    let mutable doc = Map.empty
    let mutable currentSection = ""
    let mutable error = None

    while error.IsNone && not (ParserState.atEnd state) do
        skipBlankLinesAndComments state

        if not (ParserState.atEnd state) then
            let c = ParserState.current state

            if c = '[' then
                // Section header
                match parseSectionHeader state with
                | Ok section -> currentSection <- section
                | Error e -> error <- Some e
                skipToEndOfLine state
            elif isBareKeyChar c || c = '"' then
                // Key-value pair
                match parseKeyValue state with
                | Ok (key, value) ->
                    let fullKey = if currentSection = "" then key else currentSection + "." + key
                    doc <- Map.add fullKey value doc
                | Error e -> error <- Some e
                skipToEndOfLine state
            else
                // Unknown, skip line
                skipToEndOfLine state

    match error with
    | Some e -> Error $"TOML parse error at position {state.Position}: {e}"
    | None -> Ok doc

/// Get a string value from the document
let getString (key: string) (doc: TomlDocument) : string option =
    match Map.tryFind key doc with
    | Some (TomlString s) -> Some s
    | _ -> None

/// Get a string list from an array value
let getStringList (key: string) (doc: TomlDocument) : string list option =
    match Map.tryFind key doc with
    | Some (TomlArray arr) ->
        arr |> List.choose (function TomlString s -> Some s | _ -> None) |> Some
    | _ -> None

/// Get a nested value from an inline table
let getInlineTable (key: string) (doc: TomlDocument) : Map<string, TomlValue> option =
    match Map.tryFind key doc with
    | Some (TomlInlineTable t) -> Some t
    | _ -> None

/// Get path from an inline table like { path = "/some/path" }
let getPathFromInlineTable (key: string) (doc: TomlDocument) : string option =
    match getInlineTable key doc with
    | Some table ->
        match Map.tryFind "path" table with
        | Some (TomlString s) -> Some s
        | _ -> None
    | None -> None

/// Get an integer value from the document
let getInt (key: string) (doc: TomlDocument) : int64 option =
    match Map.tryFind key doc with
    | Some (TomlInt i) -> Some i
    | _ -> None

/// Get a boolean value from the document
let getBool (key: string) (doc: TomlDocument) : bool option =
    match Map.tryFind key doc with
    | Some (TomlBool b) -> Some b
    | _ -> None

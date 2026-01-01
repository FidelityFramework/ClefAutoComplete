/// Native LSP Path Tests
/// Tests for .fidproj projects and .fsnx scripts
module FsNativeAutoComplete.Tests.NativeTests

open Expecto
open System.IO
open FsNativeAutoComplete.Core
open FsNativeAutoComplete.Core.ProjectKind
open FsNativeAutoComplete.Core.FidprojLoader
open FsNativeAutoComplete.Core.FsniDirectives

let testCasesDir = Path.Combine(__SOURCE_DIRECTORY__, "TestCases", "NativeTests")

/// Path to Firefly sample projects for real-world validation
let fireflySamplesDir = "/home/hhh/repos/Firefly/samples/console/FidelityHelloWorld"

/// Tests for ProjectKind detection
let projectKindTests =
    testList "ProjectKind" [
        testCase "detects .fidproj as Native" <| fun _ ->
            let kind = detectFromPath "test.fidproj"
            Expect.equal kind ProjectKind.Native "Should detect .fidproj as Native"

        testCase "detects .fsproj as Standard" <| fun _ ->
            let kind = detectFromPath "test.fsproj"
            Expect.equal kind ProjectKind.Standard "Should detect .fsproj as Standard"

        testCase "detects .fsnx as Native script" <| fun _ ->
            let isNative = isNativeScript "test.fsnx"
            Expect.isTrue isNative "Should detect .fsnx as native script"

        testCase "detects .fsx as NOT native script" <| fun _ ->
            let isNative = isNativeScript "test.fsx"
            Expect.isFalse isNative "Should NOT detect .fsx as native script"

        testCase "isNativeWorkspaceFile recognizes .fidproj" <| fun _ ->
            let isNative = isNativeWorkspaceFile "project.fidproj"
            Expect.isTrue isNative "Should recognize .fidproj"

        testCase "isNativeWorkspaceFile recognizes .fs in native context" <| fun _ ->
            // Note: this depends on context - the function checks file extension
            let isNative = isNativeWorkspaceFile "Main.fs"
            // .fs files are native workspace files (source files)
            Expect.isTrue isNative "Should recognize .fs files"
    ]

/// Tests for FidprojLoader
let fidprojLoaderTests =
    testList "FidprojLoader" [
        testCase "can parse test .fidproj" <| fun _ ->
            let fidprojPath = Path.Combine(testCasesDir, "HelloNative.fidproj")
            if File.Exists(fidprojPath) then
                match load fidprojPath with
                | Ok options ->
                    Expect.equal options.Name "HelloNative" "Should parse package name"
                    Expect.equal options.MemoryModel MemoryModel.StackOnly "Should parse memory model"
                    Expect.isNonEmpty options.SourceFiles "Should have source files"
                | Error e ->
                    failtest $"Failed to load .fidproj: {e}"
            else
                skiptest $"Test fixture not found: {fidprojPath}"

        testCase "findAllProjects discovers .fidproj files" <| fun _ ->
            if Directory.Exists(testCasesDir) then
                let projects = findAllProjects testCasesDir
                Expect.isNonEmpty projects "Should find at least one .fidproj"
                Expect.all projects (fun p -> p.EndsWith(".fidproj")) "All should be .fidproj files"
            else
                skiptest $"Test directory not found: {testCasesDir}"
    ]

/// Tests for FsniDirectives
let fsniDirectivesTests =
    testList "FsniDirectives" [
        testCase "parses #target directive" <| fun _ ->
            let source = """#target "x86_64-unknown-linux-gnu"
let x = 1"""
            let result = parseDirectives source
            Expect.isNonEmpty result.Directives "Should parse directive"
            match result.Directives |> List.head with
            | FsniDirective.Target triple ->
                Expect.equal triple "x86_64-unknown-linux-gnu" "Should parse target triple"
            | _ -> failtest "Expected Target directive"

        testCase "parses #memory_model directive" <| fun _ ->
            let source = "#memory_model stack_only"
            let result = parseDirectives source
            match result.Directives |> List.tryHead with
            | Some (FsniDirective.MemoryModel model) ->
                Expect.equal model MemoryModelDirective.StackOnly "Should parse stack_only"
            | _ -> failtest "Expected MemoryModel directive"

        testCase "parses #arena directive" <| fun _ ->
            let source = "#arena 4096"
            let result = parseDirectives source
            match result.Directives |> List.tryHead with
            | Some (FsniDirective.Arena size) ->
                Expect.equal size 4096 "Should parse arena size"
            | _ -> failtest "Expected Arena directive"

        testCase "parses #require directive" <| fun _ ->
            let source = """#require "alloy" """
            let result = parseDirectives source
            match result.Directives |> List.tryHead with
            | Some (FsniDirective.Require dep) ->
                Expect.equal dep "alloy" "Should parse dependency"
            | _ -> failtest "Expected Require directive"

        testCase "parses #platform directive" <| fun _ ->
            let source = """#platform "stm32l5" """
            let result = parseDirectives source
            match result.Directives |> List.tryHead with
            | Some (FsniDirective.Platform template) ->
                Expect.equal template "stm32l5" "Should parse platform"
            | _ -> failtest "Expected Platform directive"

        testCase "parses multiple directives" <| fun _ ->
            let source = """#target "thumbv8m.main-none-eabihf"
#memory_model arena
#arena 4096
#require "alloy"

let x = 1"""
            let result = parseDirectives source
            Expect.equal (List.length result.Directives) 4 "Should parse 4 directives"
            Expect.isEmpty result.Errors "Should have no errors"

        testCase "removes directives from source" <| fun _ ->
            let source = """#target "native"
let x = 1
let y = 2"""
            let result = parseDirectives source
            // Directive line should be replaced with empty line
            Expect.isFalse (result.SourceWithoutDirectives.Contains("#target")) "Should remove directive"
            Expect.isTrue (result.SourceWithoutDirectives.Contains("let x = 1")) "Should keep code"

        testCase "reports unknown directive as error" <| fun _ ->
            let source = "#unknown_directive value"
            let result = parseDirectives source
            Expect.isNonEmpty result.Errors "Should report error"

        testCase "can load test .fsnx script" <| fun _ ->
            let scriptPath = Path.Combine(testCasesDir, "test_script.fsnx")
            if File.Exists(scriptPath) then
                match loadScript scriptPath with
                | Ok options ->
                    Expect.equal options.Target "x86_64-unknown-linux-gnu" "Should parse target"
                    Expect.equal options.MemoryModel MemoryModelDirective.StackOnly "Should parse memory model"
                    Expect.contains options.Dependencies "alloy" "Should have alloy dependency"
                | Error e ->
                    failtest $"Failed to load script: {e}"
            else
                skiptest $"Test fixture not found: {scriptPath}"

        testCase "can load embedded target script" <| fun _ ->
            let scriptPath = Path.Combine(testCasesDir, "embedded_script.fsnx")
            if File.Exists(scriptPath) then
                match loadScript scriptPath with
                | Ok options ->
                    Expect.equal options.Target "thumbv8m.main-none-eabihf" "Should parse embedded target"
                    Expect.equal options.MemoryModel MemoryModelDirective.Arena "Should parse arena model"
                    Expect.equal options.ArenaSize (Some 4096) "Should parse arena size"
                    Expect.equal options.MaxStackSize (Some 2048) "Should parse max stack"
                    Expect.equal options.Platform (Some "stm32l5") "Should parse platform"
                | Error e ->
                    failtest $"Failed to load script: {e}"
            else
                skiptest $"Test fixture not found: {scriptPath}"
    ]

/// Tests against real Firefly sample projects
let fireflyIntegrationTests =
    testList "Firefly Integration" [
        testCase "can parse HelloWorldDirect.fidproj" <| fun _ ->
            let fidprojPath = Path.Combine(fireflySamplesDir, "01_HelloWorldDirect", "HelloWorldDirect.fidproj")
            if File.Exists(fidprojPath) then
                match load fidprojPath with
                | Ok options ->
                    Expect.equal options.Name "HelloWorldDirect" "Should parse package name"
                    Expect.equal options.MemoryModel MemoryModel.StackOnly "Should parse memory model"
                    Expect.equal options.Target "native" "Should parse target"
                    Expect.isNonEmpty options.SourceFiles "Should have source files"
                    Expect.contains options.SourceFiles "01_HelloWorldDirect.fs" "Should have main source file"
                    Expect.equal options.OutputName (Some "hello_direct") "Should parse output name"
                    Expect.equal options.OutputKind OutputKind.Freestanding "Should parse output kind"
                | Error e ->
                    failtest $"Failed to load HelloWorldDirect.fidproj: {e}"
            else
                skiptest $"Firefly sample not found: {fidprojPath}"

        testCase "can parse HelloWorldSaturated.fidproj" <| fun _ ->
            let fidprojPath = Path.Combine(fireflySamplesDir, "02_HelloWorldSaturated", "HelloWorldSaturated.fidproj")
            if File.Exists(fidprojPath) then
                match load fidprojPath with
                | Ok options ->
                    Expect.equal options.Name "HelloWorldSaturated" "Should parse package name"
                    Expect.equal options.MemoryModel MemoryModel.StackOnly "Should parse memory model"
                    Expect.isNonEmpty options.SourceFiles "Should have source files"
                    Expect.contains options.SourceFiles "02_HelloWorldSaturated.fs" "Should have main source file"
                    Expect.equal options.OutputName (Some "hello_saturated") "Should parse output name"
                | Error e ->
                    failtest $"Failed to load HelloWorldSaturated.fidproj: {e}"
            else
                skiptest $"Firefly sample not found: {fidprojPath}"

        testCase "discovers all FidelityHelloWorld .fidproj files" <| fun _ ->
            if Directory.Exists(fireflySamplesDir) then
                let projects = findAllProjects fireflySamplesDir
                Expect.isGreaterThanOrEqual (List.length projects) 2 "Should find at least 2 .fidproj files"
                Expect.all projects (fun p -> p.EndsWith(".fidproj")) "All should be .fidproj files"
            else
                skiptest $"Firefly samples directory not found: {fireflySamplesDir}"

        testCase "validates Alloy dependency paths exist" <| fun _ ->
            let fidprojPath = Path.Combine(fireflySamplesDir, "01_HelloWorldDirect", "HelloWorldDirect.fidproj")
            if File.Exists(fidprojPath) then
                match load fidprojPath with
                | Ok options ->
                    // Check that Alloy dependency path is valid
                    match options.Dependencies |> List.tryFind (fun d -> d.Name = "alloy") with
                    | Some dep ->
                        match dep.Path with
                        | Some p ->
                            Expect.isTrue (Directory.Exists(p)) $"Alloy path should exist: {p}"
                        | None -> failtest "Expected Alloy to have a path"
                    | None -> failtest "Expected alloy dependency"
                | Error e ->
                    failtest $"Failed to load .fidproj: {e}"
            else
                skiptest $"Firefly sample not found: {fidprojPath}"
    ]

/// All native tests
let allNativeTests =
    testList "Native LSP Path" [
        projectKindTests
        fidprojLoaderTests
        fsniDirectivesTests
        fireflyIntegrationTests
    ]

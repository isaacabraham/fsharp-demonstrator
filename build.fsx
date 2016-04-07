#I @"packages/FAKE/tools/"
#r @"FakeLib.dll"

open Fake
open Fake.Azure
open System
open System.IO

Environment.CurrentDirectory <- __SOURCE_DIRECTORY__
let solutionFile = "Demonstrator.sln"

Target "BuildSolution" (fun _ ->
    solutionFile
    |> MSBuildHelper.build (fun defaults ->
        { defaults with
            Verbosity = Some Minimal
            Targets = [ "Build" ]
            Properties = [ "Configuration", "Release"
                           "OutputPath", Kudu.deploymentTemp ] })
    |> ignore)

Target "StageWebsiteAssets" (fun _ ->
    let blacklist =
        [ "typings"
          ".fs"
          ".config"
          ".references"
          "tsconfig.json" ]
    let shouldInclude (file:string) =
        blacklist
        |> Seq.forall(not << file.Contains)
    Kudu.stageFolder (Path.GetFullPath @"src\webhost") shouldInclude)

Target "StageWebJob" (fun _ ->
    [ @"src\Sample.fsx" ]
    |> Kudu.stageWebJob Kudu.WebJobType.Continuous "sample")

Target "Deploy" Kudu.kuduSync

"StageWebsiteAssets"
==> "BuildSolution"
==> "StageWebJob"
==> "Deploy"


RunTargetOrDefault "Deploy"
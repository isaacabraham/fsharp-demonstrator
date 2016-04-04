// --------------------------------------------------------------------------------------
// FAKE build script
// --------------------------------------------------------------------------------------

#I @"packages/FAKE/tools/"
#r @"FakeLib.dll"

open Fake
open System

Environment.CurrentDirectory <- __SOURCE_DIRECTORY__

let solutionFile = "Demonstrator.sln"

let deploymentTemp = getBuildParamOrDefault "DEPLOYMENT_TEMP" @"C:\temp\foo"
let deploymentTarget = getBuildParamOrDefault "DEPLOYMENT_TARGET" @"C:\temp\bar"
let nextManifestPath = getBuildParamOrDefault "NEXT_MANIFEST_PATH" @"C:\temp\foo"
let previousManifestPath = getBuildParamOrDefault "PREVIOUS_MANIFEST_PATH" @"C:\temp\foo"
let kuduPath = getBuildParam "GO_WEB_CONFIG_TEMPLATE" |> directory

Target "Clean" (fun _ ->
    CreateDir deploymentTemp
    CreateDir deploymentTarget
    CleanDir deploymentTemp)

Target "BuildSolution" (fun _ ->
    solutionFile
    |> MSBuildHelper.build (fun defaults ->
        { defaults with
            Verbosity = Some Minimal
            Targets = [ "Rebuild" ]
            Properties = [ "Configuration", "Release"
                           "OutputPath", deploymentTemp ] })
    |> ignore)

Target "CopyWebsite" (fun _ ->
    !! @"src\webhost\**"
    -- @"src\webhost\typings"
    -- @"src\webhost\**\*.fs"
    -- @"src\webhost\**\*.config"
    -- @"src\webhost\**\*.references"
    -- @"src\webhost\tsconfig.json"
    |> FileHelper.CopyFiles deploymentTemp)

Target "DeployWebJob" (fun _ ->
    let webjobPath = deploymentTemp + @"\app_data\jobs\continuous\Sample\"
    CreateDir webjobPath
    @"src\Sample.fsx" |> FileHelper.CopyFile webjobPath)

Target "DeployWebsite" (fun _ ->
    let succeeded, output =
        ProcessHelper.ExecProcessRedirected(fun psi ->
            psi.FileName <- combinePaths kuduPath "kudusync"
            psi.Arguments <- sprintf """-v 50 -f "%s" -t "%s" -n "%s" -p "%s" -i ".git;.hg;.deployment;deploy.cmd""" deploymentTemp deploymentTarget nextManifestPath previousManifestPath)
            TimeSpan.MaxValue
    output |> Seq.iter (fun cm -> printfn "%O: %s" cm.Timestamp cm.Message)
    if not succeeded then failwith "Error occurred during Kudu deployment.")

"Clean"
==> "CopyWebsite"
==> "BuildSolution"
==> "DeployWebJob"
==> "DeployWebsite"


RunTargetOrDefault "DeployWebsite"
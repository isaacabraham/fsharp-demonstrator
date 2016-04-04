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
let deploymentTarget = getBuildParam "DEPLOYMENT_TARGET"
let nextManifestPath = getBuildParam "NEXT_MANIFEST_PATH"
let previousManifestPath = getBuildParam "PREVIOUS_MANIFEST_PATH"
let appData = getBuildParam "appData"
                     
Target "Clean" (fun _ ->
    CreateDir deploymentTemp
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
    ProcessHelper.ExecProcess(fun psi ->
        psi.FileName <- "kudusync"
        psi.WorkingDirectory <- appData + @"\npm\"
        psi.UseShellExecute <- true
        psi.Arguments <- sprintf """-v 50 -f "%s" -t "%s" -n "%s" -p "%s" -i ".git;.hg;.deployment;deploy.cmd""" deploymentTemp deploymentTarget nextManifestPath previousManifestPath)
        TimeSpan.MaxValue
    |> ignore)

"Clean"
==> "CopyWebsite"
==> "BuildSolution"
==> "DeployWebJob"
==> "DeployWebsite"

RunTargetOrDefault "DeployWebsite"
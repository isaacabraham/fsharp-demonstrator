module SuaveHost.Main

open Apps
open AppInsightsHelpers
open Helpers
open Suave
open Suave.Filters
open Suave.Operators
open System
open System.Diagnostics
open System.IO
open System.Net

let buildApp staticFilesPath : WebPart =
    let staticFileRoot = Path.GetFullPath(Environment.CurrentDirectory + staticFilesPath)
    choose [
        footballApp
        enigmaApp
        sudokuApp
        basicApp staticFileRoot
        pageNotFound staticFileRoot
    ] >=> log logger logFormat
    |> withRequestTracking

[<EntryPoint>]
let main [| port; staticFilesLocation |] =
    startTracing()
    applyAzureEnvironmentToConfigurationManager()
    
    Trace.TraceInformation (sprintf "Static Files Location: %s" staticFilesLocation)
    Trace.TraceInformation (sprintf "AppInsightsKey = %s" (Helpers.getSetting "AppInsightsKey"))

    let config =
        { defaultConfig with
            bindings = [ HttpBinding.mk HTTP IPAddress.Loopback (uint16 port) ]
            listenTimeout = TimeSpan.FromMilliseconds 3000. }
    let app = buildApp staticFilesLocation
    startWebServer config app
    0

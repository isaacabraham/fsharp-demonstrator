module SuaveHost.Main

open Apps
open AppInsightsHelpers
open Applications
open Suave
open Suave.Filters
open Suave.Operators
open System
open System.Diagnostics
open System.IO

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
    
    Trace.TraceInformation (sprintf "Static Files Location: %s" staticFilesLocation)
    Trace.TraceInformation (sprintf "AppInsightsKey = %s" (Helpers.getSetting "AppInsightsKey"))

    let config = getConfig (uint16 port)
    let app = buildApp staticFilesLocation
    startWebServer config app
    0

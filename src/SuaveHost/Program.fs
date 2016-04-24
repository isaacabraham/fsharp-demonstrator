module SuaveHost.Main

open Apps
open Helpers
open Suave
open Suave.Azure
open Suave.Filters
open Suave.Operators
open System
open System.Configuration
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
    |> ApplicationInsights.withRequestTracking ApplicationInsights.buildApiOperationName

[<EntryPoint>]
let main [| port; staticFilesLocation |] =
    Configuration.Azure.applyAzureEnvironmentToConfigurationManager()
    Tracing.Azure.addAzureAppServicesTraceListeners()
    ApplicationInsights.startMonitoring
        { AppInsightsKey = Helpers.getSetting "AppInsightsKey"
          DeveloperMode = false
          TrackDependencies = true }
    startTracing()    
    
    Trace.TraceInformation (sprintf "Static Files Location: %s" staticFilesLocation)
    Trace.TraceInformation (sprintf "AppInsightsKey = %s" (Helpers.getSetting "AppInsightsKey"))

    let config =
        { defaultConfig with
            bindings = [ HttpBinding.mk HTTP IPAddress.Loopback (uint16 port) ]
            listenTimeout = TimeSpan.FromMilliseconds 3000. }
    let app = buildApp staticFilesLocation
    startWebServer config app
    0

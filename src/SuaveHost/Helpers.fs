[<AutoOpen>]
module SuaveHost.Helpers

open Applications
open Microsoft.ApplicationInsights.DataContracts
open Microsoft.ApplicationInsights.TraceListener
open Newtonsoft.Json
open Suave
open Suave.Azure.ApplicationInsights
open Suave.Logging
open Suave.Operators
open Suave.Successful
open Suave.Writers
open System.Diagnostics
open System.Configuration
open System.Text

///// Starts all trace listeners.
let startTracing() =
    Trace.AutoFlush <- true
    Trace.Listeners.Add (new ConsoleTraceListener()) |> ignore
    Trace.Listeners.Add (new ApplicationInsightsTraceListener()) |> ignore   

let getSetting (key:string) = ConfigurationManager.AppSettings.[key]

let logger =    
    { new Logger with
        member __.Log _ fline =
            let logLine = fline()
            match logLine with
            | { ``exception`` = Some exn } -> Trace.TraceError(exn.ToString())
            | { level = LogLevel.Error } -> Trace.TraceError logLine.message
            | { level = LogLevel.Warn } -> Trace.TraceWarning logLine.message
            | _ -> Trace.TraceInformation logLine.message
            Trace.Flush() }

let toJson data = (data |> JsonConvert.SerializeObject |> OK) >=> setMimeType "application/json; charset=utf-8"
let toJsonAsync data ctx =
    data
    |> Async.map toJson
    |> Async.bind(fun webPart -> webPart ctx)

let mapJson<'a,'b> (f:'a -> 'b) =
  request(fun req ->
    f (JsonConvert.DeserializeObject<'a>(Encoding.UTF8.GetString req.rawForm))
    |> toJson)

let optionallyWith handler response =
    match response with
    | Some response -> handler response
    | None -> RequestErrors.NOT_FOUND ""

do
    // Start listening for cache events and send to AI
    applicationEvent
    |> Event.add(function
        | CacheHit (cacheName, cacheKey) ->
            let eventTelemetry = EventTelemetry(Name = "Cache Hit")
            eventTelemetry.Properties.Add("Cache", cacheName)
            eventTelemetry.Properties.Add("Cache Key", cacheKey)
            telemetryClient.TrackEvent eventTelemetry
        | GenericEvent eventName -> telemetryClient.TrackEvent eventName)
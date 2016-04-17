[<AutoOpen>]
module SuaveHost.Helpers

open Applications
open Microsoft.ApplicationInsights.TraceListener
open Newtonsoft.Json
open Suave
open Suave.Logging
open Suave.Operators
open Suave.Successful
open Suave.Writers
open System
open System.Diagnostics
open System.Net
open System.Configuration
open System.Text

/// Starts all trace listeners.
let startTracing() =
    Trace.AutoFlush <- true
    Trace.Listeners.Add (new ConsoleTraceListener()) |> ignore
    Trace.Listeners.Add (new ApplicationInsightsTraceListener()) |> ignore   

    // Try to add Azure trace listeners if available.
    let azureTraceListenerTypeNames =
        [ "Drive"; "Table"; "Blob" ]
        |> List.map (sprintf "Microsoft.WindowsAzure.WebSites.Diagnostics.Azure%sTraceListener, Microsoft.WindowsAzure.WebSites.Diagnostics, Version=1.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35")

    for traceListenerTypeName in azureTraceListenerTypeNames do
        match Type.GetType(traceListenerTypeName, throwOnError = false) |> Option.ofObj with
        | Some traceListenerType ->
            try
            let listener = Activator.CreateInstance traceListenerType :?> TraceListener
            listener.Name <- traceListenerType.Name
            Trace.Listeners.Add listener |> ignore
            with _ -> ()
        | None -> ()

/// This method looks at both Application Settings and falls back to environment
/// variable. This is how App Settings look like they are exposed to executables
/// hosted in Azure App Service.
let private tryGetEnvironmentVariable = Environment.GetEnvironmentVariable >> Option.ofObj
let getSetting (setting:string) =
    ConfigurationManager.AppSettings.[setting]
    |> Option.ofObj
    |> Option.bindNone(fun _ -> tryGetEnvironmentVariable setting)
    |> Option.withDefault ""

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
    
let getConfig port =
  { defaultConfig with
      bindings = [ HttpBinding.mk HTTP IPAddress.Loopback port ]
      listenTimeout = TimeSpan.FromMilliseconds 3000. }
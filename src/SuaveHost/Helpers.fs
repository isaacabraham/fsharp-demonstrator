[<AutoOpen>]
module SuaveHost.Helpers

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
open Microsoft.ApplicationInsights.TraceListener
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
let getSetting (setting:string) =
    ConfigurationManager.AppSettings.[setting]
    |> Option.ofObj
    |> function
    | None ->
        Trace.WriteLine (sprintf "Could not location %s in App Settings, trying Environment..." setting)
        Environment.GetEnvironmentVariable setting |> Some
    | Some x -> Some x
    |> defaultArg <| ""

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
let toJsonAsync op ctx =
    async {
        let! data = op
        let data = data |> toJson
        return! data ctx
    }

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
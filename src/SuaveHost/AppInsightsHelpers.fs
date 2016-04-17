module SuaveHost.AppInsightsHelpers

open Applications
open Microsoft.ApplicationInsights
open Microsoft.ApplicationInsights.DataContracts
open Microsoft.ApplicationInsights.DependencyCollector
open Microsoft.ApplicationInsights.Extensibility
open Microsoft.ApplicationInsights.Extensibility.Implementation
open Suave
open System
open System.Configuration
open System.Diagnostics

let telemetryClient = TelemetryClient()

let private buildOperationName (uri:Uri) =
    if uri.AbsolutePath.StartsWith "/api/" && uri.Segments.Length > 2 then
        "/api/" + uri.Segments.[2]
    else uri.AbsolutePath

let withRequestTracking (webPart:WebPart) context =
    // Start recording a new operation.
    let operation = telemetryClient.StartOperation<RequestTelemetry>(buildOperationName context.request.url)
    
    async {                
        try
            try
                // Execute the webpart
                let! context = webPart context
            
                // Map the properties of the result into App Insights
                context
                |> Option.iter(fun context ->
                    operation.Telemetry.Url <- context.request.url
                    operation.Telemetry.HttpMethod <- context.request.``method``.ToString()
                    operation.Telemetry.ResponseCode <- context.response.status.code.ToString()
                    operation.Telemetry.Success <- Nullable (int context.response.status.code < 400))
            
                return context
            with ex ->
                // Hoppla! log the error and re-throw it
                let telemetry = ExceptionTelemetry(ex, HandledAt = ExceptionHandledAt.Unhandled)
                telemetryClient.TrackException telemetry
                raise ex
                return None
        finally
            telemetryClient.StopOperation operation
    }

do
    TelemetryConfiguration.Active.TelemetryChannel.DeveloperMode <- Nullable true
    TelemetryConfiguration.Active.InstrumentationKey <- Helpers.getSetting "AppInsightsKey"
    
    /// Turn on dependency tracking
    let dependencyTracking = new DependencyTrackingTelemetryModule()
    dependencyTracking.Initialize TelemetryConfiguration.Active

    // Start listening for cache events and send to AI
    applicationEvent
    |> Event.add(function
        | CacheHit (cacheName, cacheKey) ->
            let eventTelemetry = EventTelemetry(Name = "Cache Hit")
            eventTelemetry.Properties.Add("Cache", cacheName)
            eventTelemetry.Properties.Add("Cache Key", cacheKey)
            telemetryClient.TrackEvent eventTelemetry
        | GenericEvent eventName -> telemetryClient.TrackEvent eventName)
            
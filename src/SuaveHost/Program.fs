module FootballDemo.Main

open AppInsightsHelpers
open Helpers
open Suave
open Suave.Files
open Suave.Filters
open Suave.Operators
open Suave.Writers
open System
open System.IO
open LeagueTable
open System.Configuration
open System.Diagnostics

let buildApp staticFilesPath : WebPart =
    let staticFileRoot = Path.GetFullPath(Environment.CurrentDirectory + staticFilesPath)
    let browseFile = browseFile staticFileRoot
    
    choose [
        GET >=> choose [
            pathScan "/api/leaguetable/%d" (enum<FootballMonth> >> LeagueTable.getLeague >> toJsonAsync)
            pathScan "/api/team/%s" (Uri.UnescapeDataString >> TeamStats.loadStatsForTeam >> toJsonAsync)
            path "/throwAnException" >=> (fun _ -> failwith "Oh no! You've done something STUPID!"; async.Return None)
            path "/" >=> browseFile "index.html"
            browse staticFileRoot ]
        browseFile "404.html" >=> setStatus HttpCode.HTTP_404
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

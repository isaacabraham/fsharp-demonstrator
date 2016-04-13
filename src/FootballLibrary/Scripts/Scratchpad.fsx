#load "load-project-debug.fsx"

open System
open System.Collections.Generic
open Deedle
open Deedle.Internal
open FootballDemo
open FootballDemo.LeagueTable
open FootballDemo.TeamStats

do fsi.AddPrinter(fun (printer:IFsiFormattable) -> "\n" + (printer.Format()))


/// Get results for a single month
getResultsForMonth FootballMonth.September
|> Async.RunSynchronously
|> Array.filter isValidResult
|> Array.fold applyResultToTable Map.empty
|> Map.toList
|> List.map snd
|> List.sortByDescending(fun x -> x.Pts)
|> Frame.ofRecords





/// Get cumulative from August -> November
getLeague FootballMonth.October
|> Async.RunSynchronously
|> Frame.ofRecords



loadStatsForTeam "Leicester City"
|> Async.RunSynchronously













// Memoization
let basicMemoize func =
    let cachedResults = Dictionary()
    fun arg ->
        if not (cachedResults.ContainsKey arg) then cachedResults.[arg] <- func(arg)
        else printfn "CACHE HIT FOR %A" arg
        cachedResults.[arg]

let add(a,b) =
    printfn "CALLING ADD!"
    a + b

let addM = basicMemoize add

addM(5,11)










// Event hook
Applications.applicationEvent
|> Event.choose(function
| Applications.CacheHit(key, value) -> Some (key,value)
| _ -> None)
|> Event.add(printfn "CACHE HIT: %A")

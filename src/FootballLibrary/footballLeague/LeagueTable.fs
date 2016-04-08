module FootballDemo.LeagueTable

open System
open FSharp.Data
open Applications

type Results = HtmlProvider<"https://uk.sports.yahoo.com/football/premier-league/fixtures/?month=12&season=2015&year=2015">
type Result = Results.Table2.Row
type FootballMonth = | August = 0 | September = 1 | October = 2 | November = 3 | December = 4 | January = 5 | February = 6 | March = 7 | April = 8 | May = 9

let createTeam name = { Name = name; Played = 0; For = 0; Against = 0; Pts = 0; Wins = 0; Draws = 0; Losses = 0; GoalDifference = 0 }
let applyResultToTable teams (result:Result) =
    let homeTeam = result.Score
    let awayTeam = result.Column4
    let [| home; away |] = result.Away.Split '-' |> Array.map(fun score -> score.Trim() |> int)
    teams
    |> Map.add homeTeam ((teams.TryFind homeTeam |> defaultArg <| createTeam homeTeam).RecordScore(home, away))
    |> Map.add awayTeam ((teams.TryFind awayTeam |> defaultArg <| createTeam awayTeam).RecordScore(away, home))

let getYearMonth month =
    (if month >= FootballMonth.January then 2016 else 2015), ((int month + 7) % 12) + 1

let getResultsForMonth =
    getYearMonth
    >> fun (year, month) -> sprintf "https://uk.sports.yahoo.com/football/premier-league/fixtures/?month=%02d&season=2015&year=%d" month year
    >> Results.AsyncLoad
    >> Async.map(fun data -> data.Tables.Table2.Rows)
    |> memoize (TimeSpan.FromSeconds 30.) "Results for Year"

let isValidResult =
    let isBlankRow (result:Result) = String.IsNullOrWhiteSpace result.Away
    let awayRowContains text (result:Result) = result.Away.Contains text
    let isCurrentlyPlaying = awayRowContains "LIVE"
    let isDateRow = awayRowContains ","
    let isInFuture = awayRowContains "vs"
    let isPostponed = awayRowContains "P - P"
    /// Compose two rules together by ANDing the results to make a new rule
    let (<&>) ruleA ruleB result = ruleA result && ruleB result

    (not << isBlankRow)
    <&> (not << isDateRow)
    <&> (not << isInFuture)
    <&> (not << isCurrentlyPlaying)
    <&> (not << isPostponed)

let getLeague (untilMonth:FootballMonth) =
    [| int FootballMonth.August .. int untilMonth |]
    |> Array.map (enum<FootballMonth> >> getResultsForMonth)
    |> Async.Parallel
    |> Async.map
        (Array.collect id
        >> Array.filter isValidResult
        >> Array.fold applyResultToTable Map.empty
        >> Map.toList
        >> List.map snd
        >> List.sortByDescending(fun team -> team.Pts, team.GoalDifference, team.For))
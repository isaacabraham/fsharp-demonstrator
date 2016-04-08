module FootballDemo.TeamStats

open System
open FSharp.Data
open Applications

type TeamPage = HtmlProvider<"https://uk.sports.yahoo.com/football/teams/newcastle-united/players/">

let getTeamPage =
    sprintf "https://uk.sports.yahoo.com/football/teams/%s/players/"
    >> TeamPage.AsyncLoad
    >> Async.map(fun data -> data.Tables)
    |> memoize (TimeSpan.FromSeconds 30.) "Team Page"

type Player = { Name : string; Goals : float; Assists : float; Shots : float; Fouls : float }

let getStats team (teamPage:TeamPage.TablesContainer) =
    let toZero (number) = if (number < 0. || Double.IsNaN number) then 0. else number
    let players =
        (teamPage.Forwards.Rows |> Seq.map(fun r -> { Name = r.Forwards; Goals = r.G; Assists = r.GA; Shots = r.SHO; Fouls = r.FC }) |> Seq.toList)
        @ (teamPage.Midfielders.Rows |> Seq.map(fun r -> { Name = r.Midfielders; Goals = r.G; Assists = r.GA; Shots = r.SHO; Fouls = r.FC }) |> Seq.toList)
        @ (teamPage.Defenders.Rows |> Seq.map(fun r -> { Name = r.Defenders; Goals = r.G; Assists = r.GA; Shots = r.SHO; Fouls = r.FC }) |> Seq.toList)
        |> List.map(fun player -> { player with Goals = toZero player.Goals; Assists = toZero player.Assists; Shots = toZero player.Shots; Fouls = toZero player.Fouls })

    let totalShots = players |> List.sumBy(fun p -> p.Shots)
    
    { Name = team
      ShotEffectiveness =
        let totalGoals = players |> List.sumBy(fun p -> p.Goals)
        (100. / totalShots) * totalGoals
      TotalShots = int totalShots
      TopScorer = players |> List.sortByDescending(fun p -> p.Goals) |> List.map(fun p -> sprintf "%s (%d goals)" p.Name (int p.Goals)) |> List.head
      DirtiestPlayer = players |> List.sortByDescending(fun p -> p.Fouls) |> List.map(fun p -> sprintf "%s (%d fouls)" p.Name (int p.Fouls)) |> List.head
      AssistLeader = players |> List.sortByDescending(fun p -> p.Assists) |> List.map(fun p -> sprintf "%s (%d assists)" p.Name (int p.Assists)) |> List.head }

let loadStatsForTeam (teamName:string) =
    let teamName = teamName.ToLower().Replace(" ", "-")
    async {
        let! teamPage = getTeamPage teamName
        return getStats teamName teamPage
    }
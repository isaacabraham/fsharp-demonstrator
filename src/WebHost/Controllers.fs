namespace FootballDemo

open System.Web.Http

type LeagueTableController() = 
    inherit ApiController()
    member __.Get(id) = LeagueTable.getLeague id |> Async.StartAsTask

type TeamController() =
    inherit ApiController()
    member __.Get id = TeamStats.loadStatsForTeam id |> Async.StartAsTask


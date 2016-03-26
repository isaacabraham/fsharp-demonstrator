namespace FootballDemo

type TeamStat =
    { Name : string
      TotalShots : int
      ShotEffectiveness : float
      TopScorer : string
      DirtiestPlayer : string
      AssistLeader : string }

type LeagueEntry =
    { Name : string
      Played : int
      Wins : int
      Draws : int
      Losses : int
      For : int
      Against : int
      Pts : int
      GoalDifference : int  }
        
        /// Records a new score for this team.
        member this.RecordScore result =
            let scored, conceded = result
            let (|Win|Loss|Draw|) (scored, conceded) = if scored > conceded then Win elif scored = conceded then Draw else Loss

            { this with
                Played = this.Played + 1
                Wins = this.Wins + match result with | Win -> 1 | _ -> 0
                Draws = this.Draws + match result with | Draw -> 1 | _ -> 0
                Losses = this.Losses + match result with | Loss -> 1 | _ -> 0
                For = this.For + scored
                Against = this.Against + conceded
                Pts = this.Pts + match result with | Win -> 3 | Draw -> 1 | Loss -> 0
                GoalDifference = this.GoalDifference + scored - conceded }

var footballModule = angular.module('footballModule', ['ui.bootstrap']);

interface Team { Name: string; Played: number; Wins: number; Draws: number; Losses: number; For: number; Against: number; Pts: number; GoalDifference: number; Style : string }
interface Month { Name : string; Id : number }
interface TeamStats { Name : string; TotalShots : number; ShotEffectiveness : number; TopScorer : string; DirtiestPlayer : string; AssistLeader : string }
interface LeagueScope extends ng.IScope { Controller : LeagueTableController }

class LeagueTableController {
    Teams: Team[];
    Months : Month[];
    SelectedMonth : Month;
    
    constructor(private httpService : ng.IHttpService, private scope : ng.IScope, private modalService : ng.ui.bootstrap.IModalService) {
        this.Months = [ "August", "September", "October", "November", "December", "January", "February", "March", "April", "May" ].map((month, index) => <Month>{ Name: month, Id: index });
        this.Teams = [];
        this.SelectedMonth = this.Months[0];
        this.loadTable(this.SelectedMonth.Id);
    }
    
    private getStyle(index:number) {
        if (index == 0)
            return "success";
        else if (index < 4)
            return "info";
        else if (index == 4)
            return "warning";
        else if (index > 16)
            return "danger";
    }
        
    
    loadTable(selectedMonth : number) {
        this.httpService
                .get("api/leaguetable/" + selectedMonth)
                .success((data: Team[]) =>
                {
                    data.map((team, index) => team.Style = this.getStyle(index));
                    this.Teams = data;
                });
    }
    
    loadTeam(selectedTeam : string) {
        var modalInstance = this.modalService.open({
            animation: true,
            templateUrl: 'partials/team-stats.html',
            controller: 'TeamStatsCtrl',
            resolve : { teamName : () => selectedTeam }
            });
    }
}

footballModule.controller('LeagueTableCtrl', ($scope: LeagueScope, $uibModal : ng.ui.bootstrap.IModalService,  $http: ng.IHttpService) => {
    $scope.Controller = new LeagueTableController($http, $scope, $uibModal);
});

footballModule.controller('TeamStatsCtrl', ($scope: any, $http: ng.IHttpService, teamName) => {
    $scope.LoadingData = true;
    $scope.TeamName = teamName;
    $http.get("api/team/" + teamName).success((data: TeamStats) => { 
        $scope.Stats = data;
        $scope.LoadingData = false;
    });
});
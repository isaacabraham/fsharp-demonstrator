var footballModule = angular.module('footballModule', ['ui.bootstrap']);
var LeagueTableController = (function () {
    function LeagueTableController(httpService, scope, modalService) {
        this.httpService = httpService;
        this.scope = scope;
        this.modalService = modalService;
        this.Months = ["August", "September", "October", "November", "December", "January", "February", "March", "April", "May"].map(function (month, index) { return { Name: month, Id: index }; });
        this.Teams = [];
        this.SelectedMonth = this.Months[0];
        this.loadTable(this.SelectedMonth.Id);
    }
    LeagueTableController.prototype.getStyle = function (index) {
        if (index == 0)
            return "success";
        else if (index < 4)
            return "info";
        else if (index == 4)
            return "warning";
        else if (index > 16)
            return "danger";
    };
    LeagueTableController.prototype.loadTable = function (selectedMonth) {
        var _this = this;
        this.httpService
            .get("api/leaguetable/" + selectedMonth)
            .success(function (data) {
            data.map(function (team, index) { return team.Style = _this.getStyle(index); });
            _this.Teams = data;
        });
    };
    LeagueTableController.prototype.loadTeam = function (selectedTeam) {
        var modalInstance = this.modalService.open({
            animation: true,
            templateUrl: 'partials/team-stats.html',
            controller: 'TeamStatsCtrl',
            resolve: { teamName: function () { return selectedTeam; } }
        });
    };
    return LeagueTableController;
})();
footballModule.controller('LeagueTableCtrl', function ($scope, $uibModal, $http) {
    $scope.Controller = new LeagueTableController($http, $scope, $uibModal);
});
footballModule.controller('TeamStatsCtrl', function ($scope, $http, teamName) {
    $scope.LoadingData = true;
    $scope.TeamName = teamName;
    $http.get("api/team/" + teamName).success(function (data) {
        $scope.Stats = data;
        $scope.LoadingData = false;
    });
});
//# sourceMappingURL=football.js.map
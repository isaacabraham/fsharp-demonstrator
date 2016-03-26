var fsharpDemoApp = angular.module('fsharpDemoApp', ['ngRoute', 'footballModule']);
fsharpDemoApp.config(['$routeProvider',
    function ($routeProvider) {
        $routeProvider.
            when('/league', {
            templateUrl: 'partials/league-table.html',
            controller: 'LeagueTableCtrl'
        });
    }]);
//# sourceMappingURL=app.js.map
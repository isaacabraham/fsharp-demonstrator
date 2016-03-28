var fsharpDemoApp = angular.module('fsharpDemoApp', ['ngRoute', 'footballModule', 'enigmaModule']);
fsharpDemoApp.config(['$routeProvider',
    function ($routeProvider) {
        $routeProvider.
            when('/league', {
            templateUrl: 'partials/league-table.html',
            controller: 'LeagueTableCtrl'
        }).
            when('/enigma', {
            templateUrl: 'partials/enigma.html',
            controller: 'EnigmaCtrl'
        });
    }]);
//# sourceMappingURL=app.js.map
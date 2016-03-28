var fsharpDemoApp = angular.module('fsharpDemoApp', [ 'ngRoute', 'footballModule', 'enigmaModule' ]);

fsharpDemoApp.config(['$routeProvider',
  ($routeProvider:ng.route.IRouteProvider) => {
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
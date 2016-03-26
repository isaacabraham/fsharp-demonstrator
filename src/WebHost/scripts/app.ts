var fsharpDemoApp = angular.module('fsharpDemoApp', [ 'ngRoute', 'footballModule' ]);

fsharpDemoApp.config(['$routeProvider',
  ($routeProvider:ng.route.IRouteProvider) => {
    $routeProvider.
      when('/league', {
        templateUrl: 'partials/league-table.html',
        controller: 'LeagueTableCtrl'
      });
  }]);
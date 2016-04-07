var fsharpDemoApp = angular.module('fsharpDemoApp', [ 'ngRoute', 'footballModule', 'enigmaModule', 'sudokuModule' ]);

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
      }).
      when('/sudoku', {
        templateUrl: 'partials/sudoku.html',
        controller: 'SudokuCtrl'
      });      
  }]);
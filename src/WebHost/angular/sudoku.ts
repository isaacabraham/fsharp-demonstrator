var sudokuModule = angular.module('sudokuModule', []);

sudokuModule.directive('numbersOnly', () => {
    return {
        require: 'ngModel',
        link: (scope, element, attrs, modelCtrl:ng.INgModelController) => {
            modelCtrl.$parsers.push((inputValue) => {
                if (inputValue == undefined) return ''
                var transformedInput = inputValue.replace(/[^0-9]/g, '');
                if (transformedInput != inputValue) {
                    modelCtrl.$setViewValue(transformedInput);
                    modelCtrl.$render();
                }

                return transformedInput;
            });
        }
    };
});

class Cell {
    Value: string;
    X: number;
    Y: number;
    constructor(x: number, y: number, value: string) {
        this.Value = value;
        this.X = x;
        this.Y = y;
    }
}

class Grid {
    data: Cell[][][][];
    constructor(data: Cell[][][][]) {
        this.data = data;
    }
}

interface Solution {
    Grid: Cell[]
    Result : boolean
}

interface SudokuScope extends ng.IScope {
    grid: Grid
    solve(): void
    status: string
    clear(): void
    processing: boolean
    displayStatus: boolean
    statusClass:string
}

sudokuModule.controller("SudokuCtrl", ($scope: SudokuScope, $http: ng.IHttpService) => {
    var counter = [0, 1, 2];
    var grid = new Grid(counter.map(verticalBand =>
        counter.map(horizontalBand =>
            counter.map(line =>
                counter.map(cell => {
                    var y = verticalBand * 3 + line + 1;
                    var x = horizontalBand * 3 + cell + 1;
                    return new Cell(x, y, '');
                })))));
    $scope.displayStatus = false;
    $scope.grid = grid;
    $scope.solve = () => {
        $scope.processing = true;
        $scope.displayStatus = true;
        $scope.status = "Solving...";
        $scope.statusClass = "warning";
        $http.post("/api/sudoku/solve", JSON.stringify(grid))
            .error(x => {
                $scope.processing = false;
                $scope.statusClass = "danger";
                $scope.status = "Error!";
            })
            .success((solution: Solution) => {
                $scope.processing = false;
                if (solution.Result) {
                    $scope.status = "Success!";
                    $scope.statusClass = "success";
                    solution.Grid.forEach(cell => {
                        var verticalBand = Math.floor((cell.Y - 1) / 3) + 1
                        var horizontalBand = Math.floor((cell.X - 1) / 3) + 1
                        var line = ((cell.Y - 1) % 3) + 1
                        var cellPos = ((cell.X - 1) % 3) + 1
                        grid.data[verticalBand - 1][horizontalBand - 1][line - 1][cellPos - 1].Value = cell.Value;
                    });
                }
                else {
                    $scope.status = "Failed to solve this puzzle :(";
                    $scope.statusClass = "danger";
                }
            });
    }

    $scope.clear = () => {
        grid.data.forEach((verticalBand, a, b) =>
            verticalBand.forEach((horizontalBand, c, d) =>
                horizontalBand.forEach((line, e, f) =>
                    line.forEach((cell, g, h) =>
                        cell.Value = ""))))
        $scope.status = "";
        $scope.displayStatus = false;
    };

    $scope.clear();
});
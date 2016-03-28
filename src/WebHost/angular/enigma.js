var enigmaModule = angular.module('enigmaModule', []);
var EnigmaController = (function () {
    function EnigmaController(httpService, scope) {
        this.httpService = httpService;
        this.scope = scope;
        // Reference Data
        this.Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        this.Reflectors = [1, 2];
        this.Rotors = [1, 2, 3, 4, 5, 6, 7, 8];
        // Initial state
        this.restart();
    }
    EnigmaController.prototype.loadReflector = function (reflectorId) {
        var _this = this;
        this.httpService
            .get("api/enigma/reflector/" + reflectorId)
            .success(function (reflector) { return _this.Reflector = reflector; });
    };
    EnigmaController.prototype.loadConfiguration = function () {
        var _this = this;
        this.httpService
            .post("api/enigma/configure", this.Configuration)
            .success(function (machine) {
            _this.Right = machine.Right.Mapping;
            _this.Middle = machine.Middle.Mapping;
            _this.Left = machine.Left.Mapping;
        });
    };
    EnigmaController.prototype.translate = function () {
        var _this = this;
        var request = { Character: this.NextChar, CharacterIndex: this.Translation.length, Configuration: this.Configuration };
        this.httpService
            .post("api/enigma/translate", request)
            .success(function (response) {
            _this.Input += _this.NextChar;
            _this.NextChar = "";
            _this.Translation += response.Translation;
            _this.Right = response.MachineState.Right.Mapping;
            _this.Middle = response.MachineState.Middle.Mapping;
            _this.Left = response.MachineState.Left.Mapping;
        });
    };
    EnigmaController.prototype.restart = function () {
        this.Configuration = {
            ReflectorId: "2",
            Right: { RingSetting: "A", WheelPosition: "A", RotorId: "3" },
            Middle: { RingSetting: "A", WheelPosition: "A", RotorId: "2" },
            Left: { RingSetting: "A", WheelPosition: "A", RotorId: "1" },
            PlugBoard: []
        };
        this.Translation = "";
        this.Input = "";
        this.NextChar = "";
        this.loadReflector(parseInt(this.Configuration.ReflectorId));
        this.loadConfiguration();
    };
    return EnigmaController;
})();
enigmaModule.controller('EnigmaCtrl', function ($scope, $http) {
    $scope.Controller = new EnigmaController($http, $scope);
});
//# sourceMappingURL=enigma.js.map
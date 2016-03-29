var enigmaModule = angular.module('enigmaModule', []);
// Manages the Enigma simulator
var EnigmaController = (function () {
    function EnigmaController(httpService, scope) {
        this.httpService = httpService;
        this.scope = scope;
        // Reference Data
        this.Keyboard = this.toColouredCharacters("ABCDEFGHIJKLMNOPQRSTUVWXYZ");
        this.Reflectors = [1, 2];
        this.Rotors = [1, 2, 3, 4, 5, 6, 7, 8];
        // Initial state
        this.restart();
    }
    EnigmaController.prototype.toColouredCharacters = function (data) {
        return data.split('')
            .map(function (c) { return { Value: c, State: "" }; });
    };
    EnigmaController.prototype.clearState = function () {
        this.Translation = "";
        this.Input = "";
        this.NextChar = "";
        this.wipeColouredState(this.Keyboard);
        this.wipeColouredState(this.Right);
        this.wipeColouredState(this.Left);
        this.wipeColouredState(this.Middle);
        this.wipeColouredState(this.Reflector);
    };
    EnigmaController.prototype.loadConfiguration = function () {
        var _this = this;
        this.clearState();
        this.httpService
            .post("api/enigma/configure", this.Configuration)
            .success(function (machine) {
            _this.Right = _this.toColouredCharacters(machine.Right.Mapping);
            _this.Middle = _this.toColouredCharacters(machine.Middle.Mapping);
            _this.Left = _this.toColouredCharacters(machine.Left.Mapping);
        });
        this.httpService
            .get("api/enigma/reflector/" + this.Configuration.ReflectorId)
            .success(function (reflector) { return _this.Reflector = _this.toColouredCharacters(reflector); });
    };
    EnigmaController.prototype.findIndexOfColouredCharacter = function (mapping, position) {
        for (var index = 0; index < mapping.length; index++) {
            if (mapping[index].Value == position)
                return index;
        }
    };
    EnigmaController.prototype.wipeColouredState = function (mapping) {
        if (mapping == null)
            return;
        mapping.forEach(function (element) { return element.State = ""; });
    };
    EnigmaController.prototype.setUpwardsColour = function (mapping, position) {
        this.wipeColouredState(mapping);
        var index = this.findIndexOfColouredCharacter(this.Keyboard, position);
        mapping[index].State = "success";
        return mapping[index].Value;
    };
    EnigmaController.prototype.setInverseColour = function (mapping, position) {
        var index = this.findIndexOfColouredCharacter(mapping, position);
        mapping[index].State = "warning";
        return this.Keyboard[index].Value;
    };
    EnigmaController.prototype.uploadPlugboard = function (letter) {
        var fromPosition = this.Plugboard.indexOf(letter);
        // Deleted the letter
        if (letter.Value.length == 0) {
            var characterToBlank = this.Keyboard[fromPosition].Value;
            var indexToBlank = this.findIndexOfColouredCharacter(this.Plugboard, characterToBlank);
            this.Plugboard[indexToBlank].Value = null;
            this.clearState();
            return;
        }
        letter.Value = letter.Value.toUpperCase();
        var copyIndex = this.findIndexOfColouredCharacter(this.Keyboard, letter.Value);
        var destination = this.Keyboard[fromPosition].Value;
        // Same as input - reject.
        if (destination == letter.Value) {
            letter.Value = null;
            return;
        }
        // Remove other one
        this.Plugboard
            .filter(function (pb) { return pb.Value == letter.Value; })
            .filter(function (pb) { return pb != letter; })
            .forEach(function (pb) { return pb.Value = null; });
        // Update
        this.Plugboard[copyIndex].Value = destination;
        this.clearState();
    };
    EnigmaController.prototype.translate = function () {
        var _this = this;
        this.NextChar = this.NextChar.toUpperCase();
        var request = { Character: this.NextChar, CharacterIndex: this.Translation.length, Configuration: this.Configuration };
        this.httpService
            .post("api/enigma/translate", request)
            .success(function (response) {
            // Update machine state
            _this.Right = _this.toColouredCharacters(response.MachineState.Right.Mapping);
            _this.Middle = _this.toColouredCharacters(response.MachineState.Middle.Mapping);
            _this.Left = _this.toColouredCharacters(response.MachineState.Left.Mapping);
            // Set colours
            _this.setUpwardsColour(_this.Keyboard, _this.NextChar);
            var next = _this.setUpwardsColour(_this.Right, _this.NextChar);
            next = _this.setUpwardsColour(_this.Middle, next);
            next = _this.setUpwardsColour(_this.Left, next);
            next = _this.setUpwardsColour(_this.Reflector, next);
            next = _this.setInverseColour(_this.Left, next);
            next = _this.setInverseColour(_this.Middle, next);
            next = _this.setInverseColour(_this.Right, next);
            _this.setInverseColour(_this.Keyboard, next);
            // Update textboxes
            _this.Input += _this.NextChar;
            _this.NextChar = "";
            _this.Translation += response.Translation;
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
        this.Plugboard = this.toColouredCharacters("                          ");
        this.Plugboard.forEach(function (element) { return element.Value = null; });
        this.loadConfiguration();
        this.clearState();
    };
    return EnigmaController;
})();
enigmaModule.controller('EnigmaCtrl', function ($scope, $http) {
    $scope.Controller = new EnigmaController($http, $scope);
});
//# sourceMappingURL=enigma.js.map
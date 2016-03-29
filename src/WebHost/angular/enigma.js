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
        this.wipeColouredState(this.Plugboard);
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
        return -1;
    };
    EnigmaController.prototype.wipeColouredState = function (mapping) {
        if (mapping == null)
            return;
        mapping.forEach(function (element) { return element.State = ""; });
    };
    EnigmaController.prototype.setUpwardsColour = function (mapping, position) {
        this.wipeColouredState(mapping);
        var index = this.findIndexOfColouredCharacter(this.Keyboard, position);
        if (mapping[index].Value == null)
            return position;
        mapping[index].State = "success";
        return mapping[index].Value;
    };
    EnigmaController.prototype.setInverseColour = function (mapping, position) {
        var index = this.findIndexOfColouredCharacter(mapping, position);
        if (index == -1)
            return position;
        mapping[index].State = "warning";
        return this.Keyboard[index].Value;
    };
    EnigmaController.prototype.uploadPlugboard = function (pbItem) {
        var _this = this;
        var plugboardPos = this.Plugboard.indexOf(pbItem);
        var kbItem = this.Keyboard[plugboardPos];
        if (pbItem.Value.length == 0) {
            // First the map
            var otherItem = this.findIndexOfColouredCharacter(this.Plugboard, kbItem.Value);
            this.Plugboard[otherItem].Value = null;
            // Now the config
            var configsToRemove = this.Configuration.PlugBoard.filter(function (pb) { return pb.From == kbItem.Value || pb.To == kbItem.Value; });
            configsToRemove.forEach(function (element) {
                _this.Configuration.PlugBoard.splice(_this.Configuration.PlugBoard.indexOf(element), 1);
            });
            this.clearState();
            return;
        }
        pbItem.Value = pbItem.Value.toUpperCase();
        // Same as input - reject.
        if (kbItem.Value == pbItem.Value) {
            pbItem.Value = null;
            return;
        }
        //2. Remove any duplicates
        // first config
        var existingConfigs = this.Configuration.PlugBoard.filter(function (pb) { return pb.From == pbItem.Value || pb.To == pbItem.Value; });
        existingConfigs.forEach(function (element) {
            _this.Configuration.PlugBoard.splice(_this.Configuration.PlugBoard.indexOf(element), 1);
            console.log(_this.Configuration.PlugBoard.length);
        });
        // then the map
        this.Plugboard
            .filter(function (pb) { return pb.Value == pbItem.Value; })
            .filter(function (pb) { return pb != pbItem; })
            .forEach(function (pb) { return pb.Value = null; });
        //3. Add new item
        // first config
        this.Configuration.PlugBoard.push({ From: kbItem.Value, To: pbItem.Value });
        // then the map
        var kbPos = this.findIndexOfColouredCharacter(this.Keyboard, pbItem.Value);
        this.Plugboard[kbPos].Value = kbItem.Value;
        // Every time you change this, it's a new configuration - start over.
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
            var next = _this.setUpwardsColour(_this.Keyboard, _this.NextChar);
            next = _this.setUpwardsColour(_this.Plugboard, next);
            next = _this.setUpwardsColour(_this.Right, next);
            next = _this.setUpwardsColour(_this.Middle, next);
            next = _this.setUpwardsColour(_this.Left, next);
            next = _this.setUpwardsColour(_this.Reflector, next);
            next = _this.setInverseColour(_this.Left, next);
            next = _this.setInverseColour(_this.Middle, next);
            next = _this.setInverseColour(_this.Right, next);
            next = _this.setInverseColour(_this.Plugboard, next);
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
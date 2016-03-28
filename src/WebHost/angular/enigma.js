var enigmaModule = angular.module('enigmaModule', []);
var EnigmaController = (function () {
    function EnigmaController(httpService, scope) {
        var _this = this;
        this.httpService = httpService;
        this.scope = scope;
        this.Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        this.loadReflector(2);
        this.loadRotor(1, function (rotor) { return _this.Right = rotor.Mapping; });
        this.loadRotor(2, function (rotor) { return _this.Middle = rotor.Mapping; });
        this.loadRotor(3, function (rotor) { return _this.Left = rotor.Mapping; });
    }
    EnigmaController.prototype.loadReflector = function (reflectorId) {
        var _this = this;
        this.httpService
            .get("api/enigma/reflector/" + reflectorId)
            .success(function (reflector) { return _this.Reflector = reflector; });
    };
    EnigmaController.prototype.loadRotor = function (rotorId, onComplete) {
        this.httpService
            .get("api/enigma/rotor/" + rotorId)
            .success(function (reflector) { return onComplete(reflector); });
    };
    return EnigmaController;
})();
enigmaModule.controller('EnigmaCtrl', function ($scope, $http) {
    $scope.Controller = new EnigmaController($http, $scope);
});
//# sourceMappingURL=enigma.js.map
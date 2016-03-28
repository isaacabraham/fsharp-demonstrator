var enigmaModule = angular.module('enigmaModule', []);

interface EnigmaScope extends ng.IScope { Controller : EnigmaController }
interface RotorResponse
    { RotorId : number
      Mapping : string
      KnockOns : number[] }
      
class EnigmaController {
    Reflector : string;
    Right : string;
    Middle : string;
    Left : string;
    Steckerboard : string;
    Alphabet : string;
    
    constructor(private httpService : ng.IHttpService, private scope : ng.IScope) {
        this.Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        this.loadReflector(2);
        this.loadRotor(1, rotor => this.Right = rotor.Mapping);
        this.loadRotor(2, rotor => this.Middle = rotor.Mapping);
        this.loadRotor(3, rotor => this.Left = rotor.Mapping);
    }
    
    loadReflector(reflectorId : number) {
        this.httpService
                .get("api/enigma/reflector/" + reflectorId)
                .success((reflector: string) => this.Reflector = reflector);
    }
    
    loadRotor(rotorId : number, onComplete:(rotor:RotorResponse) => void) {
        this.httpService
                .get("api/enigma/rotor/" + rotorId)
                .success((reflector: RotorResponse) => onComplete(reflector));
    }
}

enigmaModule.controller('EnigmaCtrl', ($scope:EnigmaScope,  $http: ng.IHttpService) => {
    $scope.Controller = new EnigmaController($http, $scope);
});

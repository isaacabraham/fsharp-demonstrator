var enigmaModule = angular.module('enigmaModule', []);

interface EnigmaScope extends ng.IScope { Controller : EnigmaController }
interface RotorState { RotorId : number; Mapping : string; KnockOns : number[] }
interface MachineState { Left : RotorState; Middle : RotorState; Right : RotorState }
interface RotorConfiguration { RotorId : string; WheelPosition : string; RingSetting : string }
interface PlugboardMapping { From : string; To : string }
interface Configuration {
    ReflectorId : string
    Left : RotorConfiguration
    Middle : RotorConfiguration
    Right : RotorConfiguration
    PlugBoard : PlugboardMapping [] }
interface TranslationRequest { Character : string; CharacterIndex : number; Configuration : Configuration }
interface TranslationResponse { Translation : string; MachineState : MachineState }
class EnigmaController {
    // Machine Internals
    Reflector : string;
    Right : string;
    Middle : string;
    Left : string;
    Steckerboard : string;
    
    // Reference Data
    Alphabet : string;
    Reflectors : number [];
    Rotors : number [];
       
    // Configuration data
    Configuration : Configuration;
    
    // Translation
    NextChar : string;
    Input : string;
    Translation : string;
    
    constructor(private httpService : ng.IHttpService, private scope : ng.IScope) {
        // Reference Data
        this.Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        this.Reflectors = [ 1, 2 ];
        this.Rotors = [ 1, 2, 3, 4, 5, 6, 7, 8 ];

        // Initial state
        this.restart();
    }
    
    loadReflector(reflectorId : number) {
        this.httpService
                .get("api/enigma/reflector/" + reflectorId)
                .success((reflector: string) => this.Reflector = reflector);
    }
    
    loadConfiguration() {
        this.httpService
                .post("api/enigma/configure", this.Configuration)
                .success((machine: MachineState) => {
                    this.Right = machine.Right.Mapping;
                    this.Middle = machine.Middle.Mapping;
                    this.Left = machine.Left.Mapping;
                });
    }
    
    translate() {
        var request = <TranslationRequest> { Character : this.NextChar, CharacterIndex : this.Translation.length, Configuration : this.Configuration };
        this.httpService
                .post("api/enigma/translate", request)
                .success((response: TranslationResponse) => {
                    this.Input += this.NextChar;
                    this.NextChar = "";
                    this.Translation += response.Translation;

                    this.Right = response.MachineState.Right.Mapping;
                    this.Middle = response.MachineState.Middle.Mapping;
                    this.Left = response.MachineState.Left.Mapping;

                });
    }
    
    restart() {
        this.Configuration = <Configuration> {
            ReflectorId : "2",
            Right : <RotorConfiguration> { RingSetting : "A", WheelPosition : "A", RotorId : "3" },
            Middle : <RotorConfiguration> { RingSetting : "A", WheelPosition : "A", RotorId : "2" },
            Left : <RotorConfiguration> { RingSetting : "A", WheelPosition : "A", RotorId : "1" },
            PlugBoard : []
        }
        this.Translation = "";
        this.Input = "";
        this.NextChar = "";

        this.loadReflector(parseInt(this.Configuration.ReflectorId));
        this.loadConfiguration();
    }
}

enigmaModule.controller('EnigmaCtrl', ($scope:EnigmaScope,  $http: ng.IHttpService) => {
    $scope.Controller = new EnigmaController($http, $scope);
});

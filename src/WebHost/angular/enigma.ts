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
interface ColouredCharacter { Value : string; State : string }
class EnigmaController {
    // Machine Internals
    Reflector : ColouredCharacter[];
    Right : ColouredCharacter[];
    Middle : ColouredCharacter[];
    Left : ColouredCharacter[];
    Keyboard : ColouredCharacter[];
    Steckerboard : string;
    
    // Reference Data
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
        this.Keyboard = this.toColouredCharacters("ABCDEFGHIJKLMNOPQRSTUVWXYZ");
        this.Reflectors = [ 1, 2 ];
        this.Rotors = [ 1, 2, 3, 4, 5, 6, 7, 8 ];

        // Initial state
        this.restart();
    }
    
    private toColouredCharacters(data:string) {
        return data.split('')
                   .map(c => <ColouredCharacter> { Value : c, State : "" });
    }
    
    loadConfiguration() {
        this.Translation = "";
        this.Input = "";
        this.NextChar = "";

        this.httpService
                .post("api/enigma/configure", this.Configuration)
                .success((machine: MachineState) => {
                    this.Right = this.toColouredCharacters(machine.Right.Mapping);
                    this.Middle = this.toColouredCharacters(machine.Middle.Mapping);
                    this.Left = this.toColouredCharacters(machine.Left.Mapping);
                });
        this.httpService
                .get("api/enigma/reflector/" + this.Configuration.ReflectorId)
                .success((reflector: string) => this.Reflector = this.toColouredCharacters(reflector));
                
        this.wipeColouredState(this.Keyboard);
    }
    
    private findIndexOfColouredCharacter(mapping:ColouredCharacter[], position:string) {
        for (var index = 0; index < mapping.length; index++) {
            if (mapping[index].Value == position)
                return index;
        }
    }
    
    private wipeColouredState(mapping:ColouredCharacter[]) { mapping.forEach(element => element.State = ""); }
    
    private setUpwardsColour(mapping:ColouredCharacter[], position:string) {
        this.wipeColouredState(mapping);
        var index = this.findIndexOfColouredCharacter(this.Keyboard, position);
        mapping[index].State = "success"
        return mapping[index].Value;
    }
    
    private setInverseColour(mapping:ColouredCharacter[], position:string) {
        var index = this.findIndexOfColouredCharacter(mapping, position);
        mapping[index].State = "warning";
        return this.Keyboard[index].Value;
    }
    
    translate() {
        this.NextChar = this.NextChar.toUpperCase();
        var request = <TranslationRequest> { Character : this.NextChar, CharacterIndex : this.Translation.length, Configuration : this.Configuration };
        this.httpService
                .post("api/enigma/translate", request)
                .success((response: TranslationResponse) => {
                    // Update machine state
                    this.Right = this.toColouredCharacters(response.MachineState.Right.Mapping);
                    this.Middle = this.toColouredCharacters(response.MachineState.Middle.Mapping);
                    this.Left = this.toColouredCharacters(response.MachineState.Left.Mapping);
                    
                    // Set colours
                    this.setUpwardsColour(this.Keyboard, this.NextChar);
                    var next = this.setUpwardsColour(this.Right, this.NextChar);
                    next = this.setUpwardsColour(this.Middle, next);
                    next = this.setUpwardsColour(this.Left, next);
                    next = this.setUpwardsColour(this.Reflector, next);
                    next = this.setInverseColour(this.Left, next);
                    next = this.setInverseColour(this.Middle, next);
                    next = this.setInverseColour(this.Right, next);
                    this.setInverseColour(this.Keyboard, next);

                    // Update textboxes
                    this.Input += this.NextChar;
                    this.NextChar = "";
                    this.Translation += response.Translation;
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
        this.loadConfiguration();
    }
}

enigmaModule.controller('EnigmaCtrl', ($scope:EnigmaScope,  $http: ng.IHttpService) => {
    $scope.Controller = new EnigmaController($http, $scope);
});

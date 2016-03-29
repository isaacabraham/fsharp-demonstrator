var enigmaModule = angular.module('enigmaModule', []);

// Types
interface EnigmaScope extends ng.IScope { Controller : EnigmaController }
interface RotorState { RotorId : number; Mapping : string; KnockOns : number[] }
interface MachineState { Left : RotorState; Middle : RotorState; Right : RotorState }
interface RotorConfiguration { RotorId : string; WheelPosition : string; RingSetting : string }
interface PlugboardMapping { From : string; To : string; }
interface Configuration {
    ReflectorId : string
    Left : RotorConfiguration
    Middle : RotorConfiguration
    Right : RotorConfiguration
    PlugBoard : PlugboardMapping [] }
interface TranslationRequest { Character : string; CharacterIndex : number; Configuration : Configuration }
interface TranslationResponse { Translation : string; MachineState : MachineState }
interface ColouredCharacter { Value : string; State : string }

// Manages the Enigma simulator
class EnigmaController {
    // Machine Internals
    Reflector : ColouredCharacter[];
    Right : ColouredCharacter[];
    Middle : ColouredCharacter[];
    Left : ColouredCharacter[];
    Keyboard : ColouredCharacter[];
    Plugboard : ColouredCharacter[];
    
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
    
    private clearState() {
        this.Translation = "";
        this.Input = "";
        this.NextChar = "";
        this.wipeColouredState(this.Keyboard);
        this.wipeColouredState(this.Right);
        this.wipeColouredState(this.Left);
        this.wipeColouredState(this.Middle);
        this.wipeColouredState(this.Plugboard);
        this.wipeColouredState(this.Reflector);
    }
    
    loadConfiguration() {
        this.clearState();
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
    }
    
    private findIndexOfColouredCharacter(mapping:ColouredCharacter[], position:string) {
        for (var index = 0; index < mapping.length; index++) {
            if (mapping[index].Value == position)
                return index;
        }
        return -1;
    }
       
    private wipeColouredState(mapping:ColouredCharacter[]) {
        if (mapping == null)
            return;
        mapping.forEach(element => element.State = "");
    }
    
    private setUpwardsColour(mapping:ColouredCharacter[], position:string) {
        this.wipeColouredState(mapping);
        var index = this.findIndexOfColouredCharacter(this.Keyboard, position);
        if (mapping[index].Value == null)
            return position;
            
        mapping[index].State = "success"
        return mapping[index].Value;
    }
    
    private setInverseColour(mapping:ColouredCharacter[], position:string) {
        var index = this.findIndexOfColouredCharacter(mapping, position);
        if (index == -1)
            return position; 
        mapping[index].State = "warning";
        return this.Keyboard[index].Value;
    }
    
    uploadPlugboard(pbItem:ColouredCharacter) {
        var plugboardPos = this.Plugboard.indexOf(pbItem);
        var kbItem = this.Keyboard[plugboardPos];

        if (pbItem.Value.length == 0)
        {
            // First the map
            var otherItem = this.findIndexOfColouredCharacter(this.Plugboard, kbItem.Value); 
            this.Plugboard[otherItem].Value = null;
            
            // Now the config
            var configsToRemove = this.Configuration.PlugBoard.filter(pb => pb.From == kbItem.Value || pb.To == kbItem.Value);
            configsToRemove.forEach(element => {
                this.Configuration.PlugBoard.splice(this.Configuration.PlugBoard.indexOf(element), 1);});            
            
            this.clearState();
            return;
        }        
       
        pbItem.Value = pbItem.Value.toUpperCase();
                                
        // Same as input - reject.
        if (kbItem.Value == pbItem.Value)
        {
            pbItem.Value = null;
            return;
        }
        
        //2. Remove any duplicates
        
        // first config
        var existingConfigs = this.Configuration.PlugBoard.filter(pb => pb.From == pbItem.Value || pb.To == pbItem.Value);
        existingConfigs.forEach(element => {
            this.Configuration.PlugBoard.splice(this.Configuration.PlugBoard.indexOf(element), 1);
            console.log(this.Configuration.PlugBoard.length);
        });
        
        // then the map
        this.Plugboard
            .filter(pb => pb.Value == pbItem.Value)
            .filter(pb => pb != pbItem)
            .forEach(pb => pb.Value = null);
       
        //3. Add new item
        // first config
        this.Configuration.PlugBoard.push(<PlugboardMapping>{ From : kbItem.Value, To : pbItem.Value });

        // then the map
        var kbPos = this.findIndexOfColouredCharacter(this.Keyboard, pbItem.Value);
        this.Plugboard[kbPos].Value = kbItem.Value;
        
        // Every time you change this, it's a new configuration - start over.
        this.clearState();
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
                    var next = this.setUpwardsColour(this.Keyboard, this.NextChar);
                    next = this.setUpwardsColour(this.Plugboard, next);
                    next = this.setUpwardsColour(this.Right, next);
                    next = this.setUpwardsColour(this.Middle, next);
                    next = this.setUpwardsColour(this.Left, next);
                    next = this.setUpwardsColour(this.Reflector, next);
                    next = this.setInverseColour(this.Left, next);
                    next = this.setInverseColour(this.Middle, next);
                    next = this.setInverseColour(this.Right, next);
                    next = this.setInverseColour(this.Plugboard, next);
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
        };
        this.Plugboard = this.toColouredCharacters("                          ");
        this.Plugboard.forEach(element => element.Value = null);
        this.loadConfiguration();
        this.clearState();
    }
}

enigmaModule.controller('EnigmaCtrl', ($scope:EnigmaScope,  $http: ng.IHttpService) => {
    $scope.Controller = new EnigmaController($http, $scope);
});

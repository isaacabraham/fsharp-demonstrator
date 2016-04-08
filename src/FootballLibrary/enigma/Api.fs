module Enigma.Api

type RotorConfiguration = { RotorId : int; WheelPosition : char; RingSetting : char }
type PlugboardMapping = { From : char; To : char }
type Configuration = {
    ReflectorId : int
    Left : RotorConfiguration
    Middle : RotorConfiguration
    Right : RotorConfiguration
    PlugBoard : PlugboardMapping array }
type TranslationRequest =
    { Character : char
      CharacterIndex : int
      Configuration : Configuration }
type RotorState =
    { RotorId : int
      Mapping : string
      KnockOns : int array }
type MachineState =
    { Left : RotorState
      Middle : RotorState
      Right : RotorState }
type TranslationResponse =
    { Translation : char
      MachineState : MachineState }

open Applications
open Enigma
open System
open System.Diagnostics

let private failIfNone msg = function | Some x -> x | None -> failwith msg
let private tryGetReflector = function
    | 1 -> Some Components.ReflectorA
    | 2 -> Some Components.ReflectorB
    | _ -> None
let private tryGetRotor rotorId = Components.Rotors |> List.tryFind(fun rotor -> rotor.ID = rotorId)
let private toRotorResponse rotor =
    { RotorId = rotor.ID
      Mapping = String rotor.Mapping
      KnockOns = rotor.KnockOns |> List.map(fun (KnockOn ko) -> ko) |> List.toArray }
let private getRotor = tryGetRotor >> failIfNone "Invalid rotor. Must be between 1 and 8."
let private getReflector = tryGetReflector >> failIfNone "Invalid Reflector. Must be 1 or 2."

/// Generates an Enigma machine from a public request.
let private toEnigma (request:TranslationRequest) =   
    let getPlugboard (plugboard:PlugboardMapping array) =
        let duplicates =
            plugboard
            |> Array.collect(fun pb -> [| pb.From; pb.To |])
            |> Array.countBy id
            |> Array.exists (snd >> ((<>) 1))
        if duplicates then failwith "Found duplicates in the Plugboard mapping."
        plugboard
        |> Array.map(fun pb -> String [| pb.From; pb.To |])
        |> String.concat " "

    { defaultEnigma with 
        Reflector = tryGetReflector request.Configuration.ReflectorId |> failIfNone "Invalid Reflector ID. Must be 1 or 2."
        Left = getRotor request.Configuration.Left.RotorId
        Middle = getRotor request.Configuration.Middle.RotorId
        Right = getRotor request.Configuration.Right.RotorId }
    |> withPlugBoard (getPlugboard request.Configuration.PlugBoard)
    |> withWheelPositions request.Configuration.Left.WheelPosition request.Configuration.Middle.WheelPosition request.Configuration.Right.WheelPosition
    |> withRingSettings request.Configuration.Left.RingSetting request.Configuration.Middle.RingSetting request.Configuration.Right.RingSetting

/// Translates an API request.
let performTranslation (request:TranslationRequest) : TranslationResponse =
    let enigma = request |> toEnigma |> moveForwardBy request.CharacterIndex
    let translatedCharacter, newEnigma = Operations.translateChar enigma request.Character

    publishEvent (GenericEvent "Translation")
    Trace.TraceInformation(sprintf "Enigma translated %c to %c" request.Character translatedCharacter)

    { Translation = translatedCharacter
      MachineState =
      { Left = newEnigma.Left |> toRotorResponse
        Middle = newEnigma.Middle |> toRotorResponse
        Right = newEnigma.Right |> toRotorResponse } }
let getReflectorResponse = tryGetReflector >> Option.map(fun (Reflector x) -> String x)
let getRotorResponse = tryGetRotor >> Option.map toRotorResponse

/// Gives back the state of an enigma machine given a configuration.
let configureEnigma (config:Configuration) =
    let enigma =
        { defaultEnigma with Reflector = getReflector config.ReflectorId }
        |> withRotors (getRotor config.Left.RotorId) (getRotor config.Middle.RotorId) (getRotor config.Right.RotorId)
        |> withWheelPositions config.Left.WheelPosition config.Middle.WheelPosition config.Right.WheelPosition
        |> withRingSettings config.Left.RingSetting config.Middle.RingSetting config.Right.RingSetting
    { MachineState.Left = enigma.Left |> toRotorResponse
      Middle = enigma.Middle |> toRotorResponse
      Right = enigma.Right |> toRotorResponse }
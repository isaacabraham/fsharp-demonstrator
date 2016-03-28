module SuaveHost.EnigmaApi

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
type RotorResponse =
    { RotorId : int
      Mapping : string
      KnockOns : int array }
type TranslationResponse =
    { Translation : char
      Left : RotorResponse
      Middle : RotorResponse
      Right : RotorResponse }

open Enigma
open System

let failIfNone msg = function | Some x -> x | None -> failwith msg

let private tryGetReflector reflectorId =
    match reflectorId with
    | 1 -> Some Components.ReflectorA
    | 2 -> Some Components.ReflectorB
    | _ -> None

let private tryGetRotor rotorId =
    Components.Rotors |> List.tryFind(fun rotor -> rotor.ID = rotorId)
let private toRotorResponse rotor =
    { RotorId = rotor.ID
      Mapping = String rotor.Mapping
      KnockOns = rotor.KnockOns |> List.map(fun (KnockOn ko) -> ko) |> List.toArray }

let getReflectorResponse = tryGetReflector >> Option.map(fun (Reflector x) -> String x)
let getRotorResponse = tryGetRotor >> Option.map toRotorResponse

/// Generates an Enigma machine from a public request.
let toEnigma (request:TranslationRequest) =   
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

    let getRotor = tryGetRotor >> failIfNone "Invalid rotor. Must be between 1 and 8."

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

    { Translation = translatedCharacter
      Left = newEnigma.Left |> toRotorResponse
      Middle = newEnigma.Middle |> toRotorResponse
      Right = newEnigma.Right |> toRotorResponse }

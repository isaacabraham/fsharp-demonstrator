#load "../../../paket-files/isaacabraham/enigma/src/Enigma/Domain.fs"
#load "../../../paket-files/isaacabraham/enigma/src/Enigma/Logic.fs"
#load "../EnigmaApi.fs"
#r "../../../packages/Newtonsoft.Json/lib/net45/Newtonsoft.Json.dll"

open SuaveHost
open SuaveHost.EnigmaApi
open System

let toPlugboardEntry (pair:string) = { From = pair.[0]; To = pair.[1] }

let config =
    { ReflectorId = 2
      Left = { RotorId = 1; WheelPosition = 'A'; RingSetting = 'A' }
      Middle = { RotorId = 2; WheelPosition = 'B'; RingSetting = 'A' }
      Right = { RotorId = 3; WheelPosition = 'R'; RingSetting = 'A' }
      PlugBoard = [| "MX" |] |> Array.map toPlugboardEntry }

let request =
    { Character = 'M'
      CharacterIndex = 0
      Configuration = config }

request |> Newtonsoft.Json.JsonConvert.SerializeObject

let reflector = getReflectorResponse 2

let printIt reflector board (response:TranslationResponse) =
    let alphabet = String [| 'A' .. 'Z' |]
    printfn "Translated to %c" response.Translation
    printfn "A: %s" alphabet
    printfn "S: %s"
        (alphabet.ToCharArray()
         |> Array.map(fun letter ->
             board
             |> Array.tryFind(fun b -> b.From = letter || b.To = letter)
             |> Option.map(fun b -> if b.From = letter then b.To else b.From)
             |> defaultArg <| ' ')
         |> String)
    printfn "R: %s" response.Right.Mapping
    printfn "M: %s" response.Middle.Mapping
    printfn "L: %s" response.Left.Mapping
    printfn "F: %s" reflector

let printItWith = printIt reflector config.PlugBoard

performTranslation request |> printItWith
performTranslation { request with Character = 'A'; CharacterIndex = 1 } |> printItWith
performTranslation { request with Character = 'B'; CharacterIndex = 2 } |> printItWith
performTranslation { request with Character = 'E'; CharacterIndex = 3 } |> printItWith
performTranslation { request with Character = 'K'; CharacterIndex = 4 } |> printItWith



module SudokuSolver.Api

open Nessos.Streams
open DomainModel
open Soduku

let processData request =
    request.Data
    |> Stream.ofArray
    |> Stream.collect (Seq.collect (Seq.collect id) >> Stream.ofSeq)
    |> Stream.map toCell
    |> Stream.toArray
    |> Solve
    |> fun (grid, succeeded) -> 
        { Grid = 
                grid
                |> Option.map (Stream.ofSeq >> Stream.map toRequest >> Stream.toArray)
                |> function 
                | Some solution -> solution
                | None -> Array.empty
          Result = succeeded }

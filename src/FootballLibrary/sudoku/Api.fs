module SudokuSolver.Api

open Nessos.Streams
open Applications
open DomainModel
open Soduku

let private publishEvent response =
    if response.Result then publishEvent (GenericEvent "Successful Sudoku solution")
    else publishEvent (GenericEvent "Failed Sudoku solution")

let solve request =
    let response =
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
    publishEvent response
    response
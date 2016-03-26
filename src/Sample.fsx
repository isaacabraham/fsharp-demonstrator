open System
open System.IO

let filename = @"D:\home\site\wwwroot\sample.txt"

printfn "Launched web job!"

while true do
    printf "Writing file %s..." (Path.GetFullPath filename)
    File.WriteAllText(filename, sprintf "%O: Hello from an F# web job!" DateTime.UtcNow)
    printfn "All done, sleeping for 30 seconds!"
    Async.Sleep 30000 |> Async.RunSynchronously
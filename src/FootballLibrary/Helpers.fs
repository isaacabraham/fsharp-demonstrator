module FootballDemo.Helpers

open System
open System.Collections.Generic
open System.Threading.Tasks

let private cacheHit = Event<string * string>()
let CacheHitEvent = cacheHit.Publish

/// A Decorator - takes in a function, decorates it and returns back a new function
/// with the same signature.
type Decorator<'a> = (string -> Async<'a>) -> string -> Async<'a>

module Async =
    let map continuation expr =
        async {
            let! values = expr
            return continuation values
        }

/// Simple, non-thread-safe, permanent memoization
let private basicMemoize func =
    let results = Dictionary()
    fun arg ->
        if not (results.ContainsKey arg) then results.[arg] <- func(arg)
        results.[arg]

/// Memoize with eviction.
let evictingMemoize ttl name func =
    let results = Dictionary()

    let tryGetValue now arg =
        match results.TryGetValue arg with
        | false, _ -> None
        | true, (cachedAt, value) when (now - cachedAt) < ttl ->
            cacheHit.Trigger (name, (arg.ToString()))
            Some value
        | true, _ -> None

    fun arg ->
        async {
            match tryGetValue DateTime.UtcNow arg with
            | None ->
                let! result = func arg
                results.[arg] <- (DateTime.UtcNow, result)
                return result
            | Some result -> return result
        }

type MemoizeCommand<'a, 'b> = Get of 'a * (Async<'b> -> unit) | Set of 'a * DateTime * 'b

/// Threadsafe memoizer with eviction.
let memoize ttl name func =
    let results = Dictionary()
    let tryGetValue now arg =
        match results.TryGetValue arg with
        | false, _ -> None
        | true, (cachedAt, value) when (now - cachedAt) < ttl ->
            cacheHit.Trigger (name, (arg.ToString()))
            Some value
        | true, _ -> None

    let (|AddToCache|GetFromCache|UpdateCache|) = function
        | Get (arg, callback) ->
            match tryGetValue DateTime.UtcNow arg with
            | None -> UpdateCache(arg, callback)
            | Some result -> GetFromCache (result, callback)
        | Set(arg,date,result) -> AddToCache(arg,date,result)

    let agent = MailboxProcessor.Start(fun mailbox ->
        async {
            while true do
                let! command = mailbox.Receive()
                match command with
                | UpdateCache (arg, callback) ->
                    let resultTask = func arg |> Async.StartAsTask
                    resultTask.ContinueWith(fun (result:Task<_>) ->
                        mailbox.Post (Set (arg, DateTime.UtcNow, result.Result)))
                        |> ignore
                    callback (resultTask |> Async.AwaitTask)
                | GetFromCache (result, callback) -> callback (result |> async.Return)
                | AddToCache (arg, date, result) -> results.[arg] <- (date, result)
        })
    fun arg -> agent.PostAndReply(fun channel -> Get (arg, channel.Reply))


module Applications

open System
open System.Diagnostics
open System.Collections.Generic
open System.Threading.Tasks

type AppEventType =
    | CacheHit of name:string * key:string
    | GenericEvent of string

let private appEvent = Event<AppEventType>()
let internal publishEvent = appEvent.Trigger
let applicationEvent = appEvent.Publish

do
    applicationEvent.Add(function
        | CacheHit (cache,key) -> Trace.TraceInformation(sprintf "Cache Hit: %s, %s" cache key)
        | GenericEvent event -> Trace.TraceInformation(sprintf "Generic Event: %s" event))
        

module Async =
    let bind continuation expr =
        async {
            let! values = expr
            return! continuation values
        }

    let map continuation = bind (continuation >> async.Return)

module Option =
    /// Binds an option function if the supplied option is None. 
    let bindNone whenNone = function
    | None -> whenNone()
    | Some x -> Some x     

    /// Maps an option function if the supplied option is None. 
    let mapNone whenNone = bindNone (whenNone >> Option.Some)
    
    /// Wrapper around defaultArg operator
    let withDefault defaultValue option = defaultArg option defaultValue

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
            publishEvent (CacheHit(name, (arg.ToString())))
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
            publishEvent (CacheHit(name, (arg.ToString())))
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


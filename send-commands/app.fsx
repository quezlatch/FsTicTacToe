#r "packages/Suave/lib/net40/Suave.dll"
#r "packages/Newtonsoft.Json/lib/net45/Newtonsoft.Json.dll"

open Suave
open Suave.Http
open Suave.Operators
open Suave.Filters
open Suave.Successful
open Suave.Writers

open System

let config =
    { defaultConfig with 
        bindings = [ HttpBinding.mk HTTP System.Net.IPAddress.Any 8080us]
    }

[<AutoOpen>]
module Json =
  open Newtonsoft.Json

  let toJsonBytes value =
    value |> JsonConvert.SerializeObject |> System.Text.Encoding.UTF8.GetBytes
    
  let fromJson<'a> json =
    JsonConvert.DeserializeObject(json, typeof<'a>) :?> 'a

  let getResourceFromReq<'a> (req : HttpRequest) =
    let getString rawForm =
      System.Text.Encoding.UTF8.GetString(rawForm)
    req.rawForm |> getString |> fromJson<'a>

#r "packages/EventStore.Client/lib/net40/EventStore.ClientAPI.dll"
module EventSourcing =
  open EventStore.ClientAPI

  type MetaData = 
    {
      timestamp: DateTime
    }

  let conn =
    let conn = EventStoreConnection.Create("ConnectTo=tcp://admin:changeit@eventstore:1113; HeartBeatTimeout=500")
    conn.ConnectAsync() |> Async.AwaitTask |> Async.RunSynchronously
    conn

  let createEventData eventType event =
      let data = event |> toJsonBytes
      let metaData = { timestamp = DateTime.Now } |> toJsonBytes
      new EventData(Guid.NewGuid(), eventType, true, data, metaData)

  let appendToGameStream (sessionId:Guid) eventType expectedVersion event = async {
      let streamId = "game-" + (sessionId.ToString "N")
      let eventData = createEventData eventType event
      let! wf = conn.AppendToStreamAsync(streamId, expectedVersion, eventData) |> Async.AwaitTask        
      return wf.LogPosition.PreparePosition
  }        

  let appendToNewStream (sessionId:Guid) eventType event = appendToGameStream (sessionId:Guid) eventType ExpectedVersion.NoStream event

module EventResponse =
  let setLocationToEvent sessionId positionPromise =
    fun (ctx : HttpContext) ->
      async {
        let! position = positionPromise
        let location = sprintf "/games/%A/stream/%d" sessionId position
        return! setHeader "Location" location ctx
      }

  let eventCreated sessionId positionPromise = 
    setLocationToEvent sessionId positionPromise
    >=> Successful.created [||]

module Command =
  open EventSourcing
  
  type StartGame = 
    {
      player1: string
      player2: string  }

  type MovePlayer = {
    player: string
    position: int * int
  }

  type GameEvent =
    | GameStarted of StartGame
    | PlayerMoved of MovePlayer

  let startGame sessionId req =
    let createEventFromCmd cmd = GameStarted(cmd)
    getResourceFromReq req
    |> createEventFromCmd
    |> appendToNewStream sessionId "gameStarted"
    |> EventResponse.eventCreated sessionId

  let processor (command,id) =
    printfn "process command"
    match System.Guid.TryParse id with
    | true,sessionId -> 
      request (startGame sessionId) 
    | _ -> failwith "Could not parse id"

let app =
  POST  
  >=> pathScan "/api/commands/%s/%s" Command.processor
  >=> setMimeType "application/json; charset=utf-8"

printfn "starting commands"
startWebServer config app
#r "packages/Suave/lib/net40/Suave.dll"
#r "packages/EventStore.Client/lib/net40/EventStore.ClientAPI.dll"
   
open Suave
open Suave.Http
open Suave.Operators
open Suave.Filters
open Suave.Successful
open Suave.Writers

open EventStore.ClientAPI

open System

let conn =
  let conn = EventStoreConnection.Create("ConnectTo=tcp://admin:changeit@eventstore:1113; HeartBeatTimeout=500")
  conn.ConnectAsync() |> Async.AwaitTask |> Async.RunSynchronously
  conn

let config =
    { defaultConfig with 
        bindings = [ HttpBinding.mk HTTP System.Net.IPAddress.Any 8080us]
    }

module Events =
  type GameStarted = {
    player1: string
    player2: string
  }

  type PlayerMoved = {
    player: string
    position: int * int
  }

let processCommand (command,id) =
  match System.Guid.TryParse id with
  | true,g -> 
    fun ctx ->
      async {
        let streamId = "game-" + (g.ToString "N")
        let data = [||] : byte array
        let metaData = [||] : byte array
        let eventData = new EventData(Guid.NewGuid(), command, true, data, metaData)
        let! wf = conn.AppendToStreamAsync(streamId, ExpectedVersion.NoStream, eventData) |> Async.AwaitTask        



        return! OK "{}" ctx
      }
  | _ -> failwith "Could not parse id"

let app =
  POST  
  >=> pathScan "/api/commands/%s/%s" processCommand
  >=> setMimeType "application/json; charset=utf-8"

startWebServer config app
#r "packages/Suave/lib/net40/Suave.dll"
#r "packages/EventStore.Client/lib/net40/EventStore.ClientAPI.dll"
   
open Suave
open Suave.Http
open Suave.Operators
open Suave.Filters
open Suave.Successful
open Suave.Writers

let config =
    { defaultConfig with 
        bindings = [ HttpBinding.mk HTTP System.Net.IPAddress.Any 8080us]
    }

let processCommand (command,id) =
  match System.Guid.TryParse id with
  | true,g -> 
    fun ctx ->
      async {
        let g =
          System.DateTime.Now.ToString() 
          |> sprintf "{\"command\": \"%s\", \"received\": %A}" command
        return! OK g ctx
      }
  | _ -> failwith "Could not parse id"

let app =
  GET  
  >=> pathScan "/api/commands/%s/%s" processCommand
  >=> setMimeType "application/json; charset=utf-8"

startWebServer config app
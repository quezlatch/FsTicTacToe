#r "packages/Suave/lib/net40/Suave.dll"
    
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

let generateGuid =
  fun ctx ->
    async {
      let g = System.Guid.NewGuid() |> sprintf "{\"id\": \"%A\"}"
      return! OK g ctx
    }

let app =
  GET >=> path "/api/guid"
  >=> generateGuid
  >=> setMimeType "application/json; charset=utf-8"

startWebServer config app
module HelloWorldSuave.App

open System
open System.Security.Principal

open Suave
open Suave.Types
open Suave.Http
open Suave.Http.Applicatives
open Suave.Http.Successful
open Suave.Http.RequestErrors

open NodaTime

open HawkNet

let app (user_repo : string -> HawkCredential) =
  choose [
    url "/" >>= Files.browse_file' "index.html"
    url "/login" >>= Session.session_support (TimeSpan.FromMinutes 30.)
    Files.browse'
    url "/api_key" >>= OK "TODO"
    url "/api/secret" (* hawk authenticate this*) >>= OK "09 F9 11 02 9D 74 E3 5B D8 41 56 C5 63 56 88 C0"
    ]
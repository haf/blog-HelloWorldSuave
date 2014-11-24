module HelloWorldSuave.App

open Suave
open Suave.Http
open Suave.Http.Applicatives
open Suave.Http.Successful

let app =
  choose [
    url "/" >>= Files.browse_file' "index.html"
    url "/secret" >>= OK "Secret"
    Files.browse'
    ]
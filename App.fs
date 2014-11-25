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
let bind_hawk_request (skew : Duration)
                      f_credential
                      (req : HttpRequest)
                      : Choice<IPrincipal, string> =
  let bisect (s : string) (on : char) =
    let pi = s.IndexOf on
    if pi = -1 then None else
    Some ( s.Substring(0, pi), s.Substring(pi + 1, s.Length - pi - 1) )

  let parse_auth_header (s : string) =
    match bisect s ' ' with
    | None -> Choice2Of2 (sprintf "Couldn't split '%s' into two parts on space" s)
    | Some (scheme, parameters) -> Choice1Of2 (scheme.TrimEnd(':'), parameters)

  binding {
    let! scheme, parameter = header "authorization" parse_auth_header req
    if scheme <> "Hawk" then
      return! Choice2Of2 (sprintf "Invalid authorization scheme '%s'" scheme)
    else
      let! host         = header "host" Choice1Of2 req
      //let! content_type = header "content-type" Choice1Of2 req
      let! uri          = Parse.uri req.url
      let contents      = fun () -> UTF8.to_string' req.raw_form
      return!
        Hawk.Authenticate(
          parameter,
          host,
          req.``method``,
          uri,
          f_credential,
          auth_conf.ts_skew.ToTimeSpan().TotalMinutes |> int,
          Func<_> contents)
          //, content_type)
        |> Choice1Of2
  }

/// Authenticate the request with the HawkCredential
let authenticate (skew : Duration)
                 (f_credential : string -> HawkCredential)
                 f_err
                 : WebPart =
  let cred_callback = Func<_, _> f_credential
  bind_req (bind_hawk_request auth_conf cred_callback) f_cont f_err

let authenticate' skew f_credential =
  authenticate skew f_credential UNAUTHORIZED

//let app (user_repo : string -> HawkCredential) =
//  choose [
//    url "/" >>= Files.browse_file' "index.html"
//    url "/login" >>= Session.session_support (TimeSpan.FromMinutes 30.)
//    Files.browse'
//    url "/api_key" >>= OK "TODO"
//    url "/api/secret" (* hawk authenticate this*) >>= OK "09 F9 11 02 9D 74 E3 5B D8 41 56 C5 63 56 88 C0"
//    ]

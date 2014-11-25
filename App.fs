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

module Parse =
  open System

  let uri (s : string) =
    match Uri.TryCreate (s, UriKind.RelativeOrAbsolute) with
    | true, uri -> Choice1Of2 uri
    | false, _  -> Choice2Of2 (sprintf "Could not parse '%s' into uri" s)

// lacking Functors
module Choice =
  let map f = function
    | Choice1Of2 v -> Choice1Of2 (f v)
    | Choice2Of2 msg -> Choice2Of2 msg

  let bind (f : 'a -> Choice<'b, 'c>) (v : Choice<'a, 'c>) =
    match v with
    | Choice1Of2 v -> f v
    | Choice2Of2 c -> Choice2Of2 c

  let from_opt on_missing = function
    | None   -> Choice2Of2 on_missing
    | Some x -> Choice1Of2 x

  let (>>.) a f = bind f a

module Model =

  type ModelBinderBuilder() =
    member x.Bind(v, f) = Choice.bind f v
    member x.Return v = Choice1Of2 v
    member x.ReturnFrom o = o
    member x.Run(f) = f()
    member x.Combine(v, f:unit -> _) = Choice.bind f v
    member x.Delay(f : unit -> 'T) = f

  let binding = ModelBinderBuilder()

  open Suave
  open Suave.Types
  open Suave.Http
  open Suave.Http.RequestErrors

  let private _req c = c.request

  let bind f_bind (f_cont : 'a -> WebPart) (f_err : 'b -> WebPart) : WebPart =
    context (fun c -> match f_bind c with
                      | Choice1Of2 m   -> f_cont m
                      | Choice2Of2 err -> f_err err)

  let bind_req f_bind f_err =
    bind (_req >> f_bind) f_err

  /// model bind from the request with f_bind, passing the bound result to f_cont
  let bind_req' f_bind f_cont =
    bind_req f_bind f_cont BAD_REQUEST

  let header key f_bind (req : HttpRequest) =
    (req.headers %% key)
    |> Choice.from_opt (sprintf "Missing header '%s'" key)
    |> Choice.bind f_bind

  let form form_key f_bind (req : HttpRequest) =
    (form req) ^^ form_key
    |> Choice.from_opt (sprintf "Missing form field '%s'" form_key)
    |> Choice.bind f_bind

  let qs qs_key f_bind (req : HttpRequest) =
    (query req) ^^ qs_key
    |> Choice.from_opt (sprintf "Missing query string key '%s'" qs_key)
    |> Choice.bind f_bind

  let has_qs name =
    request <| fun r ->
      match (query r) ^^ name with
      | Some _ -> never // continue running the pipeline
      | None   -> BAD_REQUEST (sprintf "missing qs param: %s" name)

  let has_form field f =
    request <| fun r ->
      match (Suave.Types.form r) ^^ field with
      | Some value -> f value >> succeed
      | None -> never

open Model

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
      let! uri          = Parse.uri "http://localhost:8083/api/secret" //req.url
      let contents      = fun () -> UTF8.to_string' req.raw_form
      return!
        Hawk.Authenticate(
          parameter,
          host,
          req.``method``,
          uri,
          f_credential,
          10,
          Func<_> contents)
          //, content_type)
        |> Choice1Of2
  }

/// Authenticate the request with the HawkCredential
let authenticate (skew : Duration)
                 (f_credential : string -> HawkCredential)
                 f_err
                 f_cont
                 : WebPart =
  let cred_callback = Func<_, _> f_credential
  bind_req (bind_hawk_request skew cred_callback) f_cont f_err

let authenticate' skew f_credential f_cont =
  authenticate skew f_credential UNAUTHORIZED f_cont 

let def_duration = Duration.FromMinutes 10L

let app user_repo =
  choose [
    url "/" >>= Files.browse_file' "index.html"
    url "/login" >>= Session.session_support (TimeSpan.FromMinutes 30.) >>= Files.browse_file' "index.html"
    Files.browse'

    url "/api_key" >>= OK "werxhqb98rpaxn39848xrunpaw3489ruxnpa98w4rxn" 
    url "/api/secret" >>=
        authenticate' def_duration user_repo (fun p -> 
            OK "09 F9 11 02 9D 74 E3 5B D8 41 56 C5 63 56 88 C0")
    RequestErrors.NOT_FOUND "Nada"
    ]
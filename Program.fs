module HelloWorldSuave.Program

open System.IO

open Suave
open Suave.Http.Successful
open Suave.Web

open Logary
open Logary.Configuration
open Logary.Targets

open Logary.Suave

open HawkNet

open Nessos.UnionArgParser

open HelloWorldSuave

type Arguments =
  | Public_Directory of string
with
  interface IArgParserTemplate with
    member s.Usage =
      match s with
      | Public_Directory _ -> "where to read public files from"

[<EntryPoint>]
let main argv =
  let parser = UnionArgParser.Create<Arguments>()
  let root   = parser.Parse(argv).PostProcessResult(<@ Public_Directory @>, Path.GetFullPath)


  use logary =
    withLogary' "HelloWorldSuave" (
      // a new allow-all rule for 'console' with a 'console' target
      withRule (Rule.createForTarget "console") >> withTarget (Console.create Console.empty "console")
    )
  let logger = logary.GetLogger("HelloWorldSuave.main")

  let user_repo = fun _ -> HawkCredential()

  Logger.debug logger "Starting Web Server"
  web_server
    { default_config
      with logger      = SuaveAdapter(logger)
           home_folder = Some root }

    (App.app user_repo)
  0

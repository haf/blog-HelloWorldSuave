open Suave
open Suave.Http.Successful
open Suave.Web

open Logary
open Logary.Configuration
open Logary.Targets

[<EntryPoint>]
let main argv =
  use logary =
    withLogary' "HelloWorldSuave" (
      // a new allow-all rule for 'console' with a 'console' target
      withRule (Rule.createForTarget "console") >> withTarget (Console.create Console.empty "console")
    )
  let logger = logary.GetLogger("HelloWorldSuave.main")
  Logger.debug logger "Starting Web Server"
  web_server default_config (OK "Hello World!")
  0
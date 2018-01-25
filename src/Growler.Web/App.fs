module Growler.Main

open Suave
open Suave.Operators
open Suave.Filters
open Suave.DotLiquid
open Suave.Files
open System.Reflection
open Database
open System.IO
open System

let currentPath =
  Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)

let initDotLiquid () =
  setCSharpNamingConvention ()
  let templatesDir = Path.Combine(currentPath, "views")
  setTemplatesDir templatesDir

let serveStatic=
  let faviconPath = 
    Path.Combine(currentPath, "static", "images", "favicon.ico")
  choose [
    pathRegex "/static/*" >=> browseHome
    path "/favicon.ico" >=> file faviconPath
  ]

[<EntryPoint>]
let main argv =
  initDotLiquid ()

  let growlerConnString = 
   Environment.GetEnvironmentVariable  "GROWLER_DB_CONN_STRING"

  let getDataContext = dataContext growlerConnString

  let app = 
    choose [
      serveStatic
      path "/" >=> page "main/home.liquid" ""
      UserRegister.Suave.webPart getDataContext
      Auth.Suave.webPart getDataContext
  ]

  startWebServer defaultConfig app
  0

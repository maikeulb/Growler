module Growler.Main

open Suave
open Suave.Successful
open Suave.Operators
open Suave.Filters
open Suave.DotLiquid
open Suave.Files
open System.Reflection
open System.IO

let currentPath =
  Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)

let initDotLiquid () =
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
  setCSharpNamingConvention ()

  let app = 
    choose [
      serveStatic
      path "/" >=> page "main/home.liquid" ""
  ]

  startWebServer defaultConfig app
  0
     

// include Fake libs
#r "./packages/FAKE/tools/FakeLib.dll"
#r "./packages/FAKE/tools/Fake.FluentMigrator.dll"
#r "./packages/Npgsql/lib/net451/Npgsql.dll"

open Fake
open Fake.FluentMigratorHelper

let buildDir  = "./build/"

let migrationsAssembly = 
  combinePaths buildDir "Growler.Db.Migrations.dll"

let appReferences  =
    !! "/**/*.csproj"
    ++ "/**/*.fsproj"

// Targets
Target "Clean" (fun _ ->
    CleanDirs [buildDir]
)

Target "BuildMigrations" (fun _ ->
  !! "src/Growler.Db.Migrations/*.fsproj"
  |> MSBuildDebug buildDir "Build" 
  |> Log "MigrationBuild-Output: "
)
let localDbConnString = @"Server=172.17.0.2;Port=5432;Database=growler;User Id=postgres;Password=P@ssw0rd!;"
let connString = 
  environVarOrDefault 
    "GROWLER_DB_CONN_STRING"
    localDbConnString
setEnvironVar "GROWLER_DB_CONN_STRING" connString
let dbConnection = ConnectionString (connString, DatabaseProvider.PostgreSQL)

Target "RunMigrations" (fun _ -> 
  MigrateToLatest dbConnection [migrationsAssembly] DefaultMigrationOptions
)

Target "Build" (fun _ ->
  !! "src/Growler.Web/*.fsproj"
  |> MSBuildDebug buildDir "Build" 
  |> Log "AppBuild-Output: "
)


Target "Run" (fun _ -> 
    ExecProcess 
        (fun info -> info.FileName <- "./build/Growler.Web.exe")
        (System.TimeSpan.FromDays 1.)
    |> ignore
)

let noFilter = fun _ -> true

let copyToBuildDir srcDir targetDirName =
  let targetDir = combinePaths buildDir targetDirName
  CopyDir targetDir srcDir noFilter

Target "Views" (fun _ ->
  copyToBuildDir "./src/Growler.Web/views" "views"
)

Target "Static" (fun _ ->
  copyToBuildDir "./src/Growler.Web/static" "static"
)


// Build order
"Clean"
  ==> "BuildMigrations"
  ==> "RunMigrations"
  ==> "Build"
  ==> "Views"
  ==> "Static"
  ==> "Run"

// start build
RunTargetOrDefault "Build"

// include Fake libs
#r "./packages/FAKE/tools/FakeLib.dll"

open Fake
let buildDir  = "./build/"
let appReferences  =
    !! "/**/*.csproj"
    ++ "/**/*.fsproj"

// Targets
Target "Clean" (fun _ ->
    CleanDirs [buildDir]
)

Target "Build" (fun _ ->
    // compile all projects below src/app/
    MSBuildDebug buildDir "Build" appReferences
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
  ==> "Build"
  ==> "Views"
  ==> "Static"
  ==> "Run"

// start build
RunTargetOrDefault "Build"

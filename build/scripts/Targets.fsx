#I "../../packages/build/FAKE/tools"
#I "../../packages/build/System.Management.Automation/lib/net45"

#r "FakeLib.dll"
#r "System.Management.Automation.dll"
#load "Products.fsx"
#load "Build.fsx"
#load "BuildConfig.fsx"
#load "Commandline.fsx"

open System
open System.Diagnostics
open System.Text
open System.IO
open System.Management.Automation
open System.Text.RegularExpressions
open Microsoft.FSharp.Reflection
open Fake
open Fake.FileHelper
open Scripts
open Fake.Testing.XUnit2
open Products
open Products.Products
open Products.Paths
open Build.Builder
open Commandline

let productsToBuild = Commandline.parse()

let productDescriptions = productsToBuild
                          |> List.map(fun p ->
                                 p.Versions 
                                 |> List.map(fun v -> sprintf "%s %s (%s)" p.Title v.FullVersion v.Source.Description)
                             )
                          |> List.concat
                          |> String.concat Environment.NewLine

if (getBuildParam "target" |> toLower <> "help") then 
    traceHeader (sprintf "Products:%s%s%s" Environment.NewLine Environment.NewLine productDescriptions)

Target "Clean" (fun _ ->
    CleanDirs [MsiBuildDir; OutDir; ResultsDir]
    productsToBuild
    |> List.iter(fun p -> CleanDirs [OutDir @@ p.Name; p.ServiceBinDir])
)

Target "DownloadProducts" (fun () ->
    productsToBuild
    |> List.iter (fun p -> p.Download())
)

Target "PatchGuids" (fun () ->
    tracefn "Making sure guids exist for %s %s" Environment.NewLine productDescriptions
    BuildConfig.versionGuid productsToBuild
)

Target "UnitTest" (fun () ->
    let unitTestBuildDir = UnitTestsDir @@ "bin" @@ "Release"

    !! (UnitTestsDir @@ "*.csproj")
    |> MSBuildRelease unitTestBuildDir "Build"
    |> Log "MsiBuild-Output: "

    !! (unitTestBuildDir @@ "*Tests.dll")
    |> xUnit2 (fun p -> { p with HtmlOutputPath = Some (ResultsDir @@ "xunit.html") })
)

Target "PruneFiles" (fun () ->
    let prune files directory =
        let keep = files |> Seq.map (fun n -> directory @@ n)
        Directory.EnumerateFiles(directory) 
        |> Seq.except keep
        |> Seq.iter File.Delete
        
    productsToBuild
    |> List.iter(fun p -> 
        p.BinDirs
        |> List.iter(fun binDir ->
            prune [
                sprintf "%s-plugin.bat" p.Name;
                sprintf "%s-env.bat" p.Name;
                sprintf "%s-translog.bat" p.Name;
                sprintf "%s-keystore.bat" p.Name
            ] binDir
        )
    )
)

Target "BuildServices" (fun () ->
    productsToBuild |> List.iter (fun p -> BuildService p)
)

Target "BuildInstallers" (fun () ->
    productsToBuild |> List.iter (fun p -> BuildMsi p)
)

Target "Release" (fun () ->
    trace "Build in Release mode. Services and MSIs will be signed."
)

Target "Integrate" (fun () ->
    // TODO: Get the version for each different project
    let versions = productsToBuild.Head.Versions 
                  |> List.map(fun v -> v.FullVersion)
    
    // last version in the list is the _target_ version    
    let version = versions |> List.last                
    let integrationTestsTargets = getBuildParamOrDefault "testtargets" "*"
    let vagrantProvider = getBuildParamOrDefault "vagrantprovider" "local"
    let previousVersion = 
        match versions.Length with
        | 1 -> ""
        | _ -> versions.[versions.Length - 2]
        
    let script = sprintf @"cd '%s'; .\Bootstrapper.ps1 -Tests '%s' -Version '%s' -PreviousVersion '%s' -VagrantProvider '%s'" 
                    IntegrationTestsDir 
                    integrationTestsTargets 
                    version 
                    previousVersion 
                    vagrantProvider
        
    trace (sprintf "Running Powershell script: '%s'" script)
    use p = PowerShell.Create()
    use output = new PSDataCollection<PSObject>()
    output.DataAdded.Add(fun data -> trace (sprintf "%O" output.[data.Index]))
    p.Streams.Verbose.DataAdded.Add(fun data -> trace (sprintf "%O" p.Streams.Verbose.[data.Index]))
    p.Streams.Debug.DataAdded.Add(fun data -> trace (sprintf "%O" p.Streams.Debug.[data.Index]))
    p.Streams.Progress.DataAdded.Add(fun data -> trace (sprintf "%O" p.Streams.Progress.[data.Index]))
    p.Streams.Warning.DataAdded.Add(fun data -> traceError (sprintf "%O" p.Streams.Warning.[data.Index]))
    p.Streams.Error.DataAdded.Add(fun data -> traceError (sprintf "%O" p.Streams.Error.[data.Index]))
    let async =
        p.AddScript(script).BeginInvoke(null, output)
              |> Async.AwaitIAsyncResult
              |> Async.Ignore
    Async.RunSynchronously async)

Target "Help" (fun () -> trace Commandline.usage)

"Clean"
  ==> "PatchGuids"
  =?> ("DownloadProducts", (not ((getBuildParam "release") = "1")))
  ==> "PruneFiles"
  =?> ("UnitTest", (not ((getBuildParam "skiptests") = "1")))
  ==> "BuildServices"
  ==> "BuildInstallers"
  ==> "Release"

"BuildInstallers"
  ==> "Integrate"

RunTargetOrDefault "BuildInstallers"
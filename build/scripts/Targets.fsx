#I "../../packages/build/FAKE/tools"
#I "../../packages/build/System.Management.Automation/lib/net45"

#r "FakeLib.dll"
#r "System.Management.Automation.dll"
#load "Products.fsx"
#load "Versioning.fsx"
#load "Build.fsx"
#load "BuildConfig.fsx"

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
open Versioning

let version = Versioning.Version
tracefn "Starting build for version %s" version

Target "Clean" (fun _ ->
    CleanDirs [MsiBuildDir; OutDir; ResultsDir]
    Products.All |> Array.iter(fun p -> CleanDirs [OutDir @@ p.Name; p.ServiceBinDir])
)

Target "DownloadProducts" (fun () ->
  if directoryExists (Elasticsearch.ExtractedDirectory version) |> not
  then 
    Product.Download Elasticsearch version
    Product.Unzip Elasticsearch version

  if directoryExists (Kibana.ExtractedDirectory version) |> not
  then 
    Product.Download Kibana version
    Product.Unzip Kibana version
)

Target "PatchGuids" (fun () ->
    tracefn "Making sure a guid exists for v%s" version
    BuildConfig.versionGuid version |> ignore
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
        for file in System.IO.Directory.EnumerateFiles(directory) do
            if keep |> Seq.exists (fun n -> n <> file) then System.IO.File.Delete(file)

    prune ["elasticsearch-plugin.bat"] (Elasticsearch.BinDir version)
    prune ["kibana-plugin.bat"] (Kibana.BinDir version)
)

Target "BuildServices" (fun () ->
    BuildService Elasticsearch false
    // BuildService Kibana false
)

Target "BuildInstallers" (fun () ->
    BuildMsi Elasticsearch false
    // BuildMsi Kibana false
)

Target "Sign" (fun () ->
    BuildService Elasticsearch true
    // BuildService Kibana true
    BuildMsi Elasticsearch true
    // BuildMsi Kibana true
)

Target "Release" (fun () ->
    trace "Building in release mode.  All files will be signed."
)

Target "Integrate" (fun () ->
    let integrationTestsTargets = getBuildParamOrDefault "testtargets" "*"
    let script = sprintf "cd '%s'; %s -Tests %s -Version %s" Paths.IntegrationTestsDir ".\Bootstrapper.ps1" integrationTestsTargets version
    trace (sprintf "Running Powershell script: '%s'" script)
    use p = PowerShell.Create()
    let output = new PSDataCollection<PSObject>()
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

"Clean"
  =?> ("DownloadProducts", (not ((getBuildParam "release") = "1")))
  ==> "PatchGuids"
  ==> "PruneFiles"
  ==> "UnitTest"
  ==> "BuildServices"
  ==> "BuildInstallers"
  ==> "Integrate"

"UnitTest"
  ==> "Sign"
  ==> "Release"

RunTargetOrDefault "BuildInstallers"

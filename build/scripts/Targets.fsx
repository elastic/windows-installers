#I "../../packages/build/FAKE.x64/tools"
#I "../../packages/build/System.Management.Automation/lib/net45"

#r "FakeLib.dll"
#r "System.Management.Automation.dll"
#load "Products.fsx"
#load "Build.fsx"
#load "BuildConfig.fsx"
#load "Commandline.fsx"

open System
open System.IO
open System.Management.Automation
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
    let prune directory =
        Directory.EnumerateFiles(directory) 
        |> Seq.where (fun f ->
                        let name = filename f
                        Path.GetExtension(f) = "" || 
                        name.StartsWith("elasticsearch-service") || 
                        name = "elasticsearch.bat")                    
        |> Seq.iter File.Delete
        
    productsToBuild
    |> List.iter(fun p -> p.BinDirs |> List.iter prune)
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
                  |> List.map(fun v -> v.RawValue)
    
    // last version in the list is the _target_ version    
    let version = versions |> List.last    
    let integrationTestsTargets = getBuildParamOrDefault "testtargets" "*"
    let vagrantProvider = getBuildParamOrDefault "vagrantprovider" "local"
    let gui = getBuildParamOrDefault "gui" "$false"
    let noDestroy = getBuildParamOrDefault "no-destroy" "$true"
    let plugins = getBuildParamOrDefault "plugins" ""

    // copy any plugins specified to build/out
    if isNotNullOrEmpty plugins then
        let pluginNames = plugins.Split([|',';';'|], StringSplitOptions.RemoveEmptyEntries)
        versions
        |> List.map(fun v ->  Commandline.parseVersion v)
        |> List.collect(fun s ->
            pluginNames 
            |> Array.map(fun p -> Paths.InDir </> (sprintf "%s-%s.zip" p s.FullVersion))
            |> Array.toList
        )
        |> List.iter(fun p ->
            match fileExists p with
            | true -> CopyFile Paths.OutDir p
            | false -> traceFAKE "%s does not exist. Will install from public url" p 
        )

    let previousVersions = 
        match versions.Length with
        | 1 -> "@()"
        | _ -> versions.[0..versions.Length - 2]
               |> List.map(fun v -> sprintf "'%s'" v)
               |> String.concat ","
               |> sprintf "@(%s)"
        
    let script = sprintf @"cd '%s'; .\Bootstrapper.ps1 -Tests '%s' -Version '%s' -PreviousVersions %s -VagrantProvider '%s' -Gui:%s -VagrantDestroy:%s" 
                    IntegrationTestsDir 
                    integrationTestsTargets 
                    version 
                    previousVersions 
                    vagrantProvider
                    gui
                    noDestroy
        
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
    Async.RunSynchronously async

    if (p.InvocationStateInfo.State = PSInvocationState.Failed) then
        failwith "PowerShell completed abnormally due to an error"
)


Target "Help" (fun () -> trace Commandline.usage)

"Clean"
  ==> "PatchGuids"
  ==> "DownloadProducts"
  ==> "PruneFiles"
  =?> ("UnitTest", (not ((getBuildParam "skiptests") = "1")))
  ==> "BuildServices"
  ==> "BuildInstallers"
  ==> "Release"

"BuildInstallers"
  ==> "Integrate"

RunTargetOrDefault "BuildInstallers"
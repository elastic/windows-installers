#I "../../packages/build/FAKE.x64/tools"
#I "../../packages/build/System.Management.Automation/lib/net45"

#r "FakeLib.dll"
#r "System.Management.Automation.dll"
#load "Paths.fsx"
#load "Products.fsx"
#load "Artifacts.fsx"
#load "BuildConfig.fsx"
#load "Commandline.fsx"
#load "Build.fsx"

open System
open System.Collections.Generic
open System.IO
open System.Management.Automation
open Fake
open Scripts
open Fake.Testing.XUnit2
open Paths
open Build
open Commandline
open Artifacts
open Feeds

let requestedArtifacts = Commandline.parse()

// Get the resolved artifacts for those requested and cache them. This is performed here
// as opposed to the result of Commandline.parse(), to allow Resolve Target to work for potentially unknown values
let artifacts =
  let cache = new List<ResolvedArtifact>()
  (fun () ->
    match cache.Count with
    | n when n > 0 -> cache |> Seq.toList 
    | _ ->
        let assetsToBuild =
            requestedArtifacts
            |> List.map findInFeeds
            |> List.choose id
        cache.AddRange(assetsToBuild)
        cache |> Seq.toList
  )

let productDescriptions = requestedArtifacts
                          |> List.map(fun p -> sprintf "%s %s (%s)" p.Product.Title (p.Version.ToString()) p.Source.Display)
                          |> String.concat Environment.NewLine
 
if (getBuildParam "target" |> toLower <> "help") then
    traceHeader (sprintf "Products:%s%s%s" Environment.NewLine Environment.NewLine productDescriptions)

Target "Clean" (fun _ ->
    CleanDirs [ MsiBuildDir; OutDir; ResultsDir ]
    artifacts ()
    |> List.filter ResolvedArtifact.IsZip
    |> List.groupBy (fun p -> p.Product)
    |> List.iter(fun (key, artifacts) -> CleanDirs [ OutDir @@ key.Name; artifacts.Head.ServiceBinDir ])
)

Target "ListArtifacts" (fun () ->
    ArtifactsFeed.GetVersions()
    |> Seq.map ArtifactsFeed.GetBuilds
    |> Seq.concat
    |> Seq.iter (fun v -> printfn "%s: %s" v.FullVersion v.BuildId)
)

Target "Resolve" (fun () ->
    let candidates = Commandline.arguments |> List.last |> split ','
    requestedArtifacts
    |> List.map findInFeeds
    |> List.mapi (fun i asset ->
        let c = candidates.[i]  
        match asset with
        | Some a -> 
            sprintf "\nRequested:\n\t%s -> %s %s %s %s (%s)\nResolved: \n\t%s %s %s\n\t%s\n\n--------\n"
                c a.Product.Name a.Version.FullVersion a.Version.BuildId
                a.Distribution.Extension a.Source.Display
                a.Product.Name a.Version.FullVersion a.Version.BuildId a.DownloadUrl 
        | None -> sprintf "\nRequested:\n\t%s -> Not a known or valid version" c )
    |> String.concat Environment.NewLine
    |> printf "Resolved versions:\n------------------\n%s"
)

Target "DownloadProducts" (fun () ->
    artifacts ()
    |> List.iter (fun p -> p.Download())
)

Target "PatchGuids" (fun () ->
    tracefn "Making sure guids exist for %s %s" Environment.NewLine productDescriptions
    artifacts ()
    |> List.filter ResolvedArtifact.IsZip
    |> BuildConfig.versionGuid 
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
        
    artifacts ()
    |> List.filter ResolvedArtifact.IsZip
    |> List.map (fun a -> a.BinDir)
    |> List.iter prune
)

Target "BuildServices" (fun () ->
    artifacts ()
    |> List.filter ResolvedArtifact.IsZip
    |> List.iter buildService
)

Target "BuildInstallers" (fun () ->
    artifacts ()
    |> List.filter ResolvedArtifact.IsZip
    |> List.iter buildMsi
)

Target "Release" (fun () ->
    trace "Build in Release mode. Services and MSIs will be signed."
)

Target "Integrate" (fun () ->
    // TODO: Get the version for each different project
    // prefix versions with build ids, if applicable
    let requestedArtifactStrings = artifacts () |> List.map(fun a -> a.RequestedArtifactInput)
    
    // last version in the list is the _target_ version    
    let version = requestedArtifactStrings |> List.last    
    let integrationTestsTargets = getBuildParamOrDefault "testtargets" "*"
    let vagrantProvider = getBuildParamOrDefault "vagrantprovider" "local"
    let gui = getBuildParamOrDefault "gui" "$false"
    let noDestroy = getBuildParamOrDefault "no-destroy" "$true"
    let plugins = getBuildParamOrDefault "plugins" ""

    // copy any plugins specified to build/out
    if isNotNullOrEmpty plugins then
        let pluginNames = plugins.Split([|',';';'|], StringSplitOptions.RemoveEmptyEntries)
        artifacts ()
        |> List.collect(fun s ->
            pluginNames 
            |> Array.map(fun p -> InDir </> (sprintf "%s-%s.zip" p s.Version.FullVersion))
            |> Array.toList
        )
        |> List.iter(fun p ->
            match fileExists p with
            | true -> CopyFile OutDir p
            | false -> traceFAKE "%s does not exist. Will install from public url" p 
        )

    // construct PowerShell array of previous versions
    let previousVersions = 
        match requestedArtifactStrings.Length with
        | 1 -> "@()"
        | _ -> requestedArtifactStrings.[0..requestedArtifactStrings.Length - 2]
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
    
    // run PowerShell script
    let async =
        p.AddScript(script).BeginInvoke(null, output)
              |> Async.AwaitIAsyncResult
              |> Async.Ignore
    Async.RunSynchronously async

    if (p.InvocationStateInfo.State = PSInvocationState.Failed) then  
        failwithf "PowerShell completed abnormally due to an error: %A" p.InvocationStateInfo.Reason
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
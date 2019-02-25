#I "../../packages/build/FAKE.x64/tools"

#r "FakeLib.dll"
#load "Paths.fsx"
#load "Products.fsx"
#load "Artifacts.fsx"
#load "BuildConfig.fsx"
#load "Commandline.fsx"
#load "Build.fsx"
#load "Tests.fsx"

open System
open System.Collections.Generic
open System.IO
open Fake
open Scripts
open Fake.Testing.XUnit2
open Artifacts
open Build
open Commandline
open Feeds
open Paths
open Tests

let requestedArtifacts = Commandline.parse()

// Get the resolved artifacts for those requested and cache them. This is performed here
// as opposed to the result of Commandline.parse(), to allow Resolve Target to work for potentially unknown values
let artifacts =
  let mutable resolved = false
  let cache = new List<ResolvedArtifact>()
  (fun () ->
    match resolved with
    | true -> cache |> Seq.toList 
    | false ->
        requestedArtifacts
        |> List.map findDownloadedOrInFeeds
        |> List.choose id
        |> cache.AddRange
        
        tracef "%i artifacts requested, %i artifacts resolved" requestedArtifacts.Length cache.Count         
        resolved <- true
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
    requestedArtifacts
    |> List.map findDownloadedOrInFeeds
    |> List.mapi (fun i asset ->
        match asset with
        | Some a -> 
            sprintf "\nRequested:\n\t%s -> %s %s %s %s (%s)\nResolved: \n\t%s %s %s\n\t%s\n\n--------\n"
                a.RequestedInput a.Product.Name a.Version.FullVersion a.Version.BuildId
                a.Distribution.Extension a.Source.Display
                a.Product.Name a.Version.FullVersion a.Version.BuildId a.DownloadUrl 
        | None ->
            match requestedArtifacts.[i].Input with
            | Path p -> sprintf "file at %s" p
            | Value v -> v
            |> sprintf "\nRequested:\n\t%s -> Not a known or valid version")
    |> String.concat Environment.NewLine
    |> printf "Resolved versions:\n------------------\n%s"
)

Target "DownloadProducts" (fun () -> artifacts () |> List.iter (fun p -> p.Download()))

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

// target zips and MSIs
Target "BuildInstallers" (fun () -> artifacts () |> List.iter buildAndCopyMsi)

Target "Release" (fun () -> trace "Build in Release mode. Services and MSIs will be signed.")

Target "Integrate" (fun () ->     
    artifacts ()
    |> List.groupBy (fun a -> a.Product)
    |> List.map snd
    |> List.iter runIntegrationTests
    
    printTestResults ()
)

Target "Help" (fun () -> printf "%s" Commandline.usage)

// Target dependencies
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
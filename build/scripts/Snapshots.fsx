#I "../../packages/build/FAKE.x64/tools"
#I @"../../packages/build/Fsharp.Data/lib/net40"
#I @"../../packages/build/FSharp.Text.RegexProvider/lib/net40"

#r @"FakeLib.dll"
#r "Fsharp.Data.dll"
#r "Fsharp.Text.RegexProvider.dll"
#r "System.Xml.Linq.dll"
#load "Paths.fsx"

open System
open System.Net
open FSharp.Data
open Fake
open Paths.Paths

ServicePointManager.SecurityProtocol <- SecurityProtocolType.Ssl3 ||| SecurityProtocolType.Tls ||| SecurityProtocolType.Tls11 ||| SecurityProtocolType.Tls12;

module Snapshots =

    let private downloadJson (url:string) =
        use webClient = new WebClient()
        webClient.DownloadString url |> JsonValue.Parse

    let GetVersions () =
        let versions = ArtifactVersionsUrl |> downloadJson
        let arrayValue = versions.GetProperty "versions"
        arrayValue.AsArray()
        |> Seq.rev
        |> Seq.map (fun x -> x.AsString())

    let getPrerelease prerelease =
        (splitStr "-" prerelease) |> Seq.head
    
    let getSnapshotName major minor patch prerelease =
        if isNullOrWhiteSpace prerelease then
            sprintf "%d.%d.%d-SNAPSHOT" major minor patch
        else
            let prerelease = getPrerelease prerelease
            sprintf "%d.%d.%d-%s-SNAPSHOT" major minor patch prerelease

    let GetVersionsFiltered major minor patch prerelease =
        let versions = ArtifactVersionsUrl |> downloadJson
        let arrayValue = versions.GetProperty "versions"
        arrayValue.AsArray()
        |> Seq.rev
        |> Seq.map (fun x -> x.AsString())
        |> Seq.filter (fun x -> x = getSnapshotName major minor patch prerelease)

    let GetSnapshotBuilds version =
       let versions = ArtifactVersionBuildsUrl version |> downloadJson
       let arrayValue = versions.GetProperty "builds"
       arrayValue.AsArray()
       |> Seq.map (fun x -> x.AsString())

    let GetSnapshotBuildAssets product version build =
       
       let major = version |> split '.' |> List.head |> int
       
       let versions = ArtifactVersionBuildUrl version build |> downloadJson
       let assets = ((((versions.GetProperty "build").GetProperty "projects").GetProperty product).GetProperty "packages")
       let asset =
           match major with
           | v when v >= 7 -> sprintf "%s-%s-windows-x86_64.zip" product version
           | _ -> sprintf "%s-%s.zip" product version
       ((assets.GetProperty asset).GetProperty "url").InnerText()
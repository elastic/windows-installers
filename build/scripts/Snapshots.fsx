#I "../../packages/build/FAKE.x64/tools"
#I @"../../packages/build/Fsharp.Data/lib/net40"
#I @"../../packages/build/FSharp.Text.RegexProvider/lib/net40"

#r @"FakeLib.dll"
#r "Fsharp.Data.dll"
#r "Fsharp.Text.RegexProvider.dll"
#r "System.Xml.Linq.dll"

open System
open System.Net
open FSharp.Data
open Fake

ServicePointManager.SecurityProtocol <- SecurityProtocolType.Ssl3 ||| SecurityProtocolType.Tls ||| SecurityProtocolType.Tls11 ||| SecurityProtocolType.Tls12;
ServicePointManager.ServerCertificateValidationCallback <- (fun _ _ _ _ -> true)

module Snapshots =

    let private urlBase = "https://artifacts-api.elastic.co/v1/versions"

    let GetVersions() =
        use webClient = new System.Net.WebClient()
        let versions = webClient.DownloadString urlBase |> JsonValue.Parse
        let arrayValue = versions.GetProperty "versions"
        arrayValue.AsArray()
        |> Seq.rev
        |> Seq.map (fun x -> x.AsString())

    let getPrerelease (prerelease:string) =
        (splitStr "-" prerelease) |> Seq.head
    
    let getSnapshotName major minor patch prerelease =
        if isNullOrWhiteSpace (prerelease) then
            sprintf "%d.%d.%d-SNAPSHOT" major minor patch
        else
            let prerelease = getPrerelease prerelease
            sprintf "%d.%d.%d-%s-SNAPSHOT" major minor patch prerelease

    let GetVersionsFiltered major minor patch prerelease =
        use webClient = new System.Net.WebClient()
        let versions = webClient.DownloadString urlBase |> JsonValue.Parse
        let arrayValue = versions.GetProperty "versions"
        arrayValue.AsArray()
        |> Seq.rev
        |> Seq.map (fun x -> x.AsString())
        |> Seq.filter (fun x -> x = getSnapshotName major minor patch prerelease)

    let GetSnapshotBuilds version = (
       use webClient = new System.Net.WebClient()
       let url = sprintf "%s/%s/builds" urlBase version
       let versions = webClient.DownloadString url |> JsonValue.Parse
       let arrayValue = versions.GetProperty "builds"
       arrayValue.AsArray()
       |> Seq.map (fun x -> x.AsString())
    )

    let GetSnapshotBuildAssets product version build = (
       use webClient = new System.Net.WebClient()
       let url = sprintf "%s/%s/builds/%s" urlBase version build
       let versions = webClient.DownloadString url |> JsonValue.Parse
       let assets = ((((versions.GetProperty "build").GetProperty "projects").GetProperty product).GetProperty "packages")
       let asset = sprintf "%s-%s.zip" product version
       ((assets.GetProperty asset).GetProperty "url").InnerText()
    )
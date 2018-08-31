#I "../../packages/build/FAKE.x64/tools"
#I @"../../packages/build/Fsharp.Data/lib/net40"
#I @"../../packages/build/FSharp.Text.RegexProvider/lib/net40"

#r @"FakeLib.dll"
#r "Fsharp.Data.dll"
#r "Fsharp.Text.RegexProvider.dll"
#r "System.Xml.Linq.dll"

open System
open System.Collections.Generic
open System.IO
open System.Text.RegularExpressions
open System.Net
open Fake
open FSharp.Data
open FSharp.Text.RegexProvider


ServicePointManager.SecurityProtocol <- SecurityProtocolType.Ssl3 ||| SecurityProtocolType.Tls ||| SecurityProtocolType.Tls11 ||| SecurityProtocolType.Tls12;
ServicePointManager.ServerCertificateValidationCallback <- (fun _ _ _ _ -> true)

module Snapshots =

    let GetVersions = (
        use webClient = new System.Net.WebClient()
        let url = "https://artifacts-api.elastic.co/v1/versions"
        let versions = webClient.DownloadString url |> JsonValue.Parse
        let arrayValue = versions.GetProperty "versions"
        arrayValue.AsArray()
        |> Seq.rev
        |> Seq.map (fun x -> x.AsString())
     )

    let GetSnapshotBuilds version = (
       use webClient = new System.Net.WebClient()
       let url = "https://artifacts-api.elastic.co/v1/versions/" + version + "/builds"
       let versions = webClient.DownloadString url |> JsonValue.Parse
       let arrayValue = versions.GetProperty "builds"
       arrayValue.AsArray()
       |> Seq.rev
       |> Seq.map (fun x -> x.AsString())
    )

    let GetSnapshotBuildAssets product version build = (
       use webClient = new System.Net.WebClient()
       let url = "https://artifacts-api.elastic.co/v1/versions/" + version + "/builds/" + build
       let versions = webClient.DownloadString url |> JsonValue.Parse
       let assets = ((((versions.GetProperty "build").GetProperty "projects").GetProperty product).GetProperty "packages")
       let asset = sprintf "%s-%s.zip" product version
       ((assets.GetProperty asset).GetProperty "url").InnerText()
    )

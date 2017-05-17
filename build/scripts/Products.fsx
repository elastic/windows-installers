#I "../../packages/build/FAKE/tools"
#I "../../packages/build/Fsharp.Data/lib/net40"
#I "../../packages/build/FSharp.Text.RegexProvider/lib/net40"

#r "FakeLib.dll"
#r "Fsharp.Data.dll"
#r "Fsharp.Text.RegexProvider.dll"
#r "System.Xml.Linq.dll"

open System
open System.Diagnostics
open System.Globalization
open System.Text
open System.IO
open System.Text.RegularExpressions
open Microsoft.FSharp.Reflection
open Fake
open Fake.FileHelper
open FSharp.Data
open FSharp.Text.RegexProvider

module Paths =

    let BuildDir = "./build/"
    let ToolsDir = BuildDir @@ "tools/"
    let InDir = BuildDir @@ "in/"
    let OutDir = BuildDir @@ "out/"
    let ResultsDir = BuildDir @@ "results/"

    let SrcDir = "./src/"
    let MsiDir = SrcDir @@ "Elastic.Installer.Msi/"
    let MsiBuildDir = MsiDir @@ "bin/Release/"

    let IntegrationTestsDir = FullName "./src/Tests/Elastic.Installer.Integration.Tests"
    let UnitTestsDir = "src/Tests/Elastic.Installer.Domain.Tests"

    let ArtifactDownloadsUrl = "https://artifacts.elastic.co/downloads"

module Products =

    [<Literal>]
    let private feedUrl = "https://www.elastic.co/downloads/past-releases/feed"

    type DownloadFeed = XmlProvider< feedUrl >

    type VersionRegex = Regex< @"^(?:\s*(?<Product>.*?)\s*)?(?<Version>(?<Major>\d+)\.(?<Minor>\d+)\.(?<Patch>\d+)(?:\-(?<Prerelease>[\w\-]+))?)$", noMethodPrefix=true >

    type ProductVersion = {
        Product: string;
        Version: string;
        Major : string;
        Minor : string;
        Patch : string;
        Prerelease: string;
    }

    type Product =
        | Elasticsearch
        | Kibana

        member this.Name =
            match this with
            | Elasticsearch -> "elasticsearch"
            | Kibana -> "kibana"

        member this.Title =
            CultureInfo.InvariantCulture.TextInfo.ToTitleCase this.Name

        member this.DownloadUrl version =
            match this with
            | Elasticsearch ->
                sprintf "%s/elasticsearch/elasticsearch-%s.zip" Paths.ArtifactDownloadsUrl version
            | Kibana -> sprintf "%s/kibana/kibana-%s-windows-x86.zip" Paths.ArtifactDownloadsUrl version

        member this.ZipFile version =
            Paths.InDir |> CreateDir
            Paths.InDir
            |> Path.GetFullPath
            |> fun f -> Path.Combine(f, sprintf "%s-%s.zip" this.Name version)

        member this.ExtractedDirectory version =
            Paths.InDir |> CreateDir
            Paths.InDir
            |> Path.GetFullPath
            |> fun f -> Path.Combine(f, sprintf "%s-%s" this.Name version)

        member this.BinDir version = Paths.InDir @@ sprintf "%s-%s/bin/" this.Name version

        member this.ServiceDir =
            Paths.SrcDir @@ this.Title @@ sprintf "Elastic.Installer.%s.Process/" this.Title

        member this.ServiceBinDir = this.ServiceDir @@ "bin/AnyCPU/Release/"

        static member Version version =
            let m = VersionRegex().Match version
            { Product = m.Product.Value;
              Version = m.Version.Value;
              Major = m.Major.Value;
              Minor = m.Minor.Value;
              Patch = m.Patch.Value;
              Prerelease = m.Prerelease.Value; }

        static member Unzip (product : Product) version =
            tracefn "Unzipping %s %s" product.Name version
            Unzip Paths.InDir (product.ZipFile version)
            match product with
                | Kibana ->
                    let original = sprintf "kibana-%s-windows-x86" version
                    if directoryExists original = false then
                        Rename (Paths.InDir @@ (sprintf "kibana-%s" version)) (Paths.InDir @@ original)
                | _ -> ()

        static member Download (product : Product) version =
            let locations = (product.DownloadUrl version, product.ZipFile version)
            match locations with
            | (_, downloaded) when File.Exists downloaded ->
                tracefn "Already downloaded %s %s" product.Name version
            | _ ->
                tracefn "Downloading %s %s" product.Name version
                use webClient = new System.Net.WebClient()
                locations |> webClient.DownloadFile
                tracefn "Done downloading %s %s" product.Name version

        static member LastFeedVersion (product : Product) =
            // TODO: disallow prereleases for moment. Make build parameter in future
            let itemIsElasticsearch itemText =
                let m = Product.Version itemText
                m.Product.ToLower() = product.Name && (isNullOrWhiteSpace m.Prerelease)

            let feed = DownloadFeed.Load feedUrl
            let firstEsLink = feed.Channel.Items |> Seq.find (fun item -> itemIsElasticsearch item.Title)
            let version = (Product.Version firstEsLink.Title).Version
            printfn "Extracted version %s from '%s'" version firstEsLink.Title
            version

    let All = [| Elasticsearch; Kibana; |]

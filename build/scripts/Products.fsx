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

    type Product =
        | Elasticsearch
        | Kibana

        member this.Name =
            match this with
            | Elasticsearch -> "elasticsearch"
            | Kibana -> "kibana"

        member this.Title =
            CultureInfo.InvariantCulture.TextInfo.ToTitleCase this.Name

    let All = [Elasticsearch; Kibana]

    type Version = {
        Product : string;
        FullVersion : string;
        Major : int;
        Minor : int;
        Patch : int;
        Prerelease : string;
    }

    type ProductVersion(product:Product, version:Version) =

        member this.Product = product;
        member this.Version = version;
        member this.Name = product.Name
        member this.Title = product.Title

        member this.DownloadUrl =
            match product with
            | Elasticsearch ->
                sprintf "%s/elasticsearch/elasticsearch-%s.zip" Paths.ArtifactDownloadsUrl this.Version.FullVersion
            | Kibana -> sprintf "%s/kibana/kibana-%s-windows-x86.zip" Paths.ArtifactDownloadsUrl this.Version.FullVersion

        member this.ZipFile =
            Paths.InDir |> CreateDir
            Paths.InDir
            |> Path.GetFullPath
            |> fun f -> Path.Combine(f, sprintf "%s-%s.zip" this.Name this.Version.FullVersion)

        member this.ExtractedDirectory =
            Paths.InDir |> CreateDir
            Paths.InDir
            |> Path.GetFullPath
            |> fun f -> Path.Combine(f, sprintf "%s-%s" this.Name this.Version.FullVersion)

        member this.BinDir = Paths.InDir @@ sprintf "%s-%s/bin/" this.Name this.Version.FullVersion

        member this.ServiceDir =
            Paths.SrcDir @@ this.Title @@ sprintf "Elastic.Installer.%s.Process/" this.Title

        member this.ServiceBinDir = this.ServiceDir @@ "bin/AnyCPU/Release/"

        member this.Unzip () =
            tracefn "Unzipping %s %s" this.Name this.Version.FullVersion
            Unzip Paths.InDir this.ZipFile
            match this.Product with
                | Kibana ->
                    let original = sprintf "kibana-%s-windows-x86" this.Version.FullVersion
                    if directoryExists original = false then
                        Rename (Paths.InDir @@ (sprintf "kibana-%s" this.Version.FullVersion)) (Paths.InDir @@ original)
                | _ -> ()

        member this.Download () =
            let locations = (this.DownloadUrl, this.ZipFile)
            match locations with
            | (_, downloaded) when File.Exists downloaded ->
                tracefn "Already downloaded %s %s" this.Name this.Version.FullVersion
            | _ ->
                tracefn "Downloading %s %s" this.Name this.Version.FullVersion
                use webClient = new System.Net.WebClient()
                locations |> webClient.DownloadFile
                tracefn "Done downloading %s %s" this.Name this.Version.FullVersion

        static member CreateFromProduct (productToVersion:Product -> Version) (product: Product)  =
            ProductVersion(product, productToVersion product)



#I "../../packages/build/FAKE/tools"

#r "FakeLib.dll"

open System
open System.Globalization
open System.Text
open System.IO
open System.Text.RegularExpressions
open Fake
open Fake.FileHelper

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
    open Paths

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
                sprintf "%s/elasticsearch/elasticsearch-%s.zip" ArtifactDownloadsUrl this.Version.FullVersion
            | Kibana -> sprintf "%s/kibana/kibana-%s-windows-x86.zip" ArtifactDownloadsUrl this.Version.FullVersion

        member this.ZipFile =
            InDir |> CreateDir
            InDir
            |> Path.GetFullPath
            |> fun f -> Path.Combine(f, sprintf "%s-%s.zip" this.Name this.Version.FullVersion)

        member this.ExtractedDirectory =
            InDir |> CreateDir
            InDir
            |> Path.GetFullPath
            |> fun f -> Path.Combine(f, sprintf "%s-%s" this.Name this.Version.FullVersion)

        member this.BinDir = InDir @@ sprintf "%s-%s/bin/" this.Name this.Version.FullVersion

        member this.ServiceDir =
            SrcDir @@ this.Title @@ sprintf "Elastic.Installer.%s.Process/" this.Title

        member this.ServiceBinDir = this.ServiceDir @@ "bin/AnyCPU/Release/"

        member this.Unzip () =
            if directoryExists this.ExtractedDirectory |> not && fileExists this.ZipFile
            then
                tracefn "Unzipping %s %s" this.Name this.Version.FullVersion
                Unzip InDir this.ZipFile
                match this.Product with
                    | Kibana ->
                        let original = sprintf "kibana-%s-windows-x86" this.Version.FullVersion
                        if directoryExists original |> not then
                            Rename (InDir @@ (sprintf "kibana-%s" this.Version.FullVersion)) (InDir @@ original)
                    | _ -> ()
            else tracefn "Extracted directory %s already exists" this.ExtractedDirectory

        member this.Download () =
            let locations = (this.DownloadUrl, this.ZipFile)
            match locations with
            | (_, downloaded) when fileExists downloaded ->
                tracefn "Already downloaded %s %s" this.Name this.Version.FullVersion
            | _ ->
                tracefn "Downloading %s %s" this.Name this.Version.FullVersion
                use webClient = new System.Net.WebClient()
                locations |> webClient.DownloadFile
                tracefn "Done downloading %s %s" this.Name this.Version.FullVersion

        static member CreateFromProduct (productToVersion:Product -> Version) (product: Product)  =
            ProductVersion(product, productToVersion product)



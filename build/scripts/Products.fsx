#I "../../packages/build/FAKE.x64/tools"

#r "FakeLib.dll"

open System.Globalization
open System.IO
open Fake
open Fake.FileHelper

module Paths =

    let BuildDir = "./build/"
    let ToolsDir = BuildDir @@ "tools/"
    let InDir = BuildDir @@ "in/"
    let OutDir = BuildDir @@ "out/"
    let ResultsDir = BuildDir @@ "results/"

    let SrcDir = "./src/"
    let ProcessHostsDir = SrcDir @@ "ProcessHosts/"
    let MsiDir = SrcDir @@ "Installer/Elastic.Installer.Msi/"
    let MsiBuildDir = MsiDir @@ "bin/Release/"

    let IntegrationTestsDir = FullName "./src/Tests/Elastic.Installer.Integration.Tests"
    let UnitTestsDir = "src/Tests/Elastic.Domain.Tests"

    let ArtifactDownloadsUrl = "https://artifacts.elastic.co/downloads"

    let StagingDownloadsUrl product version hash = 
        sprintf "https://staging.elastic.co/%s-%s/downloads/windows-installers/%s/%s-%s.msi" 
            version hash product product version

module Products =
    open Paths

    type Product =
        | Elasticsearch
        | Kibana

        member this.Name =
            match this with
            | Elasticsearch -> "elasticsearch"
            | Kibana -> "kibana"
            
        member this.AssemblyTitle =
            match this with
            | Elasticsearch -> "Elasticsearch, you know for search!"
            | Kibana -> "kibana"
            
        member this.AssemblyDescription =
            match this with
            | Elasticsearch -> "Elasticsearch is a distributed, RESTful search and analytics engine capable of solving a growing number of use cases. As the heart of the Elastic Stack, it centrally stores your data so you can discover the expected and uncover the unexpected."
            | Kibana -> "kibana"
            
        member this.AssemblyGuid =
            match this with
            | Elasticsearch -> "d4fb307f-cb1d-4026-bd28-ca1d0016d709"
            | Kibana -> "ffb9da32-12fa-4c9d-a5bd-06cddae74fd4"

        member this.Title =
            CultureInfo.InvariantCulture.TextInfo.ToTitleCase this.Name

    let All = [Elasticsearch; Kibana]

    type Source =
        | Compile
        | Released
        | BuildCandidate of hash:string

        member this.Description =
            match this with
            | Compile -> "compiled from source"
            | Released -> "official release"
            | BuildCandidate hash -> sprintf "build candidate %s" hash

    type Version = {
        Product : string;
        FullVersion : string;
        Major : int;
        Minor : int;
        Patch : int;
        Prerelease : string;
        Source : Source;
        RawValue: string;
    }

    type ProductVersions(product:Product, versions:Version list) =
        member this.Product = product
        member this.Versions = versions
        member this.Name = product.Name
        member this.Title = product.Title

        member private this.DownloadUrl (version:Version) =
            match version.Source with
            | Compile ->
                match product with
                | Elasticsearch ->
                    sprintf "%s/elasticsearch/elasticsearch-%s.zip" ArtifactDownloadsUrl version.FullVersion
                | Kibana ->               
                    sprintf "%s/kibana/kibana-%s-windows-x86.zip" ArtifactDownloadsUrl version.FullVersion 
            | Released ->
                sprintf "%s/%s/%s-%s.msi" ArtifactDownloadsUrl this.Name this.Name version.FullVersion 
            | BuildCandidate hash ->
                StagingDownloadsUrl this.Name version.FullVersion hash

        member private this.ZipFile (version:Version) =
            let fullPathInDir = InDir |> Path.GetFullPath
            Path.Combine(fullPathInDir, sprintf "%s-%s.zip" this.Name version.FullVersion)

        member private this.ExtractedDirectory (version:Version) =
            let fullPathInDir = InDir |> Path.GetFullPath            
            Path.Combine(fullPathInDir, sprintf "%s-%s" this.Name version.FullVersion)

        member this.BinDirs = 
            this.Versions
            |> List.filter (fun v -> v.Source = Compile)
            |> List.map(fun v -> InDir @@ sprintf "%s-%s/bin/" this.Name v.FullVersion)

        member this.ServiceDir =
            ProcessHostsDir @@ sprintf "Elastic.ProcessHosts.%s/" this.Title

        member this.ServiceBinDir = this.ServiceDir @@ "bin/AnyCPU/Release/"

        member this.DownloadPath (version:Version) =
            let fullPathInDir = InDir |> Path.GetFullPath 
            let releaseFile version dir =
                let downloadUrl = this.DownloadUrl version     
                Path.Combine(fullPathInDir, dir, Path.GetFileName downloadUrl)
            match version.Source with
            | Compile ->  this.ZipFile version
            | Released -> releaseFile version "releases"
            | BuildCandidate hash -> releaseFile version hash

        member this.Download () =
            this.Versions
            |> List.iter (fun version ->
                match (this.DownloadUrl version, this.DownloadPath version) with
                | (_, file) when fileExists file ->
                    tracefn "Already downloaded %s to %s" this.Name file
                | (url, file) ->
                    tracefn "Downloading %s from %s" this.Name url 
                    let targetDirectory = file |> Path.GetDirectoryName
                    if (directoryExists targetDirectory |> not) then CreateDir targetDirectory
                    use webClient = new System.Net.WebClient()
                    (url, file) |> webClient.DownloadFile
                    tracefn "Done downloading %s from %s to %s" this.Name url file 

                match version.Source with
                | Compile -> 
                    let extractedDirectory = this.ExtractedDirectory version
                    let zipFile = this.DownloadPath version
                    if directoryExists extractedDirectory |> not
                    then
                        tracefn "Unzipping %s %s" this.Name zipFile
                        Unzip InDir zipFile
                        match this.Product with
                            | Kibana ->
                                let original = sprintf "kibana-%s-windows-x86" version.FullVersion
                                if directoryExists original |> not then
                                    Rename (InDir @@ (sprintf "kibana-%s" version.FullVersion)) (InDir @@ original)
                            | _ -> ()
                    else tracefn "Extracted directory %s already exists" extractedDirectory   
                | _ -> ()
            )

        static member CreateFromProduct (productToVersion:Product -> Version list) (product: Product)  =
            ProductVersions(product, productToVersion product)



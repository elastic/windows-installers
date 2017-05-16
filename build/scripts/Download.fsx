#I "../../packages/build/FAKE/tools"
#I "../../packages/build/Fsharp.Data/lib/net40"
#I "../../packages/build/FSharp.Text.RegexProvider/lib/net40"

#r "FakeLib.dll"
#r "Fsharp.Data.dll"
#r "Fsharp.Text.RegexProvider.dll"
#r "System.Xml.Linq.dll"

namespace Scripts

module Downloader = 
    open System.IO
    open FSharp.Data
    open FSharp.Text.RegexProvider
    open System.Xml.Linq
    open Fake
    open System
    
    let private buildInFolder = "build/in"

    [<Literal>]
    let private feedUrl = "https://www.elastic.co/downloads/past-releases/feed"

    type DownloadFeed = XmlProvider< feedUrl >

    type ProductVersionRegex = Regex< @"^(?:\s*(?<Product>.*?)\s*)?(?<Version>(?<Major>\d+)\.(?<Minor>\d+)\.(?<Patch>\d+)(?:\-(?<Prerelease>\w+))?)$", noMethodPrefix=true >
    
    type Product = 
        | Elasticsearch
        | Kibana
        
        member this.Name = 
            match this with
            | Elasticsearch -> "elasticsearch"
            | Kibana -> "kibana"
        
        member this.DownloadUrl version = 
            match this with
            | Elasticsearch -> 
                sprintf "https://artifacts.elastic.co/downloads/elasticsearch/elasticsearch-%s.zip" version
            | Kibana -> sprintf "https://artifacts.elastic.co/downloads/kibana/kibana-%s-windows-x86.zip" version
        
        member this.ZipFile version = 
            buildInFolder
            |> Directory.CreateDirectory
            |> ignore
            buildInFolder
            |> Path.GetFullPath
            |> fun f -> Path.Combine(f, sprintf "%s-%s.zip" this.Name version)
    
        member this.ExtractedDirectory version = 
            buildInFolder
            |> Directory.CreateDirectory
            |> ignore
            buildInFolder
            |> Path.GetFullPath
            |> fun f -> Path.Combine(f, sprintf "%s-%s" this.Name version)

    let unzipProduct (product : Product) version = 
        tracefn "Unzipping %s %s" product.Name version 
        Unzip buildInFolder (product.ZipFile version)
        match product with
            | Kibana -> 
                let original = sprintf "kibana-%s-windows-x86" version
                if directoryExists original = false then 
                    Rename (buildInFolder @@ (sprintf "kibana-%s" version)) (buildInFolder @@ original)
            | _ -> ()

    let downloadProduct (product : Product) version = 
        let locations = (product.DownloadUrl version, product.ZipFile version)
        match locations with
        | (_, downloaded) when File.Exists downloaded -> 
            tracefn "Already downloaded %s %s" product.Name version
        | _ -> 
            tracefn "Downloading %s %s" product.Name version
            use webClient = new System.Net.WebClient()
            locations |> webClient.DownloadFile
            tracefn "Done downloading %s %s" product.Name version
    
    let lastVersion() = 
        // TODO: disallow prereleases for moment. Make build parameter in future
        let itemIsElasticsearch itemText = 
            let m = ProductVersionRegex().Match itemText
            m.Success && m.Product.Value = "Elasticsearch" && (isNullOrWhiteSpace m.Prerelease.Value)
    
        let feed = DownloadFeed.Load feedUrl
        let firstEsLink = feed.Channel.Items |> Seq.find (fun item -> itemIsElasticsearch item.Title)
        let version = (ProductVersionRegex().Match firstEsLink.Title).Version.Value
        printfn "Extracted version %s from '%s'" version firstEsLink.Title
        version
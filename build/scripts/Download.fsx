#I "../../packages/build/FAKE/tools"#I "../../packages/build/Fsharp.Data/lib/net40"#r "FakeLib.dll"#r "Fsharp.Data.dll"#r "System.Xml.Linq.dll"
namespace Scripts

module Downloader = 
    open System.IO
    open System.Text.RegularExpressions
    open FSharp.Data
    open System.Xml.Linq
    open Fake
    open System
    
    let buildInFolder = "build/in"
    let private zipLocation version = 
        sprintf "https://artifacts.elastic.co/downloads/elasticsearch/elasticsearch-%s.zip" version
    
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
        | (_, downloaded) when File.Exists downloaded -> tracefn "Already downloaded %s %s" product.Name version
        | _ -> 
            tracefn "Downloading %s %s" product.Name version
            use webClient = new System.Net.WebClient()
            locations |> webClient.DownloadFile
            tracefn "Done downloading %s %s" product.Name version
    
    //todo hardcoded? :(
    let majorVersion = "5"
    let itemIsElasticsearch itemText = 
        Regex.IsMatch(itemText, @"^(?:\s*Elasticsearch\s*)?" + majorVersion + ".\d+\.\d+")
    
    type DownloadFeed = XmlProvider< "https://www.elastic.co/downloads/past-releases/feed" >
    
    let lastVersion = 
        let feed = DownloadFeed.Load "https://www.elastic.co/downloads/past-releases/feed"
        let firstEsLink = feed.Channel.Items |> Seq.find (fun item -> itemIsElasticsearch item.Title)
        let replaced = Regex.Replace(firstEsLink.Title, "^(?:\s*Elasticsearch\s*)?(\d+\.\d+\.\d+(?:-\w+)?).*$", "$1")
        printfn "%s - ^%s$" replaced firstEsLink.Title
        replaced

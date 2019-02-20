#I "../../packages/build/FAKE.x64/tools"

#r "FakeLib.dll"
#load "Paths.fsx"

open System.Globalization
open Fake

type Distribution =
    | Msi
    | Zip
    
    member this.Extension =
        match this with
        | Msi -> "msi"
        | Zip -> "zip"
        
    static member parse (candidate:string) =
        match candidate |> toLower with
        | "msi" -> Msi
        | "zip" -> Zip
        | _ -> failwithf "Cannot parse distribution from: %s" candidate
        
let (|IsDistribution|_|) (candidate:string) =
    match candidate with
    | "msi" -> Some Msi
    | "zip" -> Some Zip
    | _ -> None

type Source = 
    | Official
    | Staging
    | Snapshot
    member this.Display =
        match this with
        | Official -> "Official release"
        | Staging -> "Build candidate for official release"
        | Snapshot -> "On demand or nightly build"
    
let (|IsSource|_|) (candidate:string) =
    match candidate with
    | "official" -> Some Official
    | "staging" -> Some Staging
    | "snapshot" -> Some Snapshot
    | _ -> None
   
type Product =
    | Elasticsearch
    | Kibana
    
    static member parse (candidate:string) =
        match candidate with
        | "e"
        | "es"
        | "elasticsearch" -> Elasticsearch
        | "k"
        | "kibana" -> Kibana
        | _ -> failwithf "Cannot parse product from: %s" candidate
        
    static member tryParse (candidate:string) =
        match candidate with
        | "e"
        | "es"
        | "elasticsearch" -> Some Elasticsearch
        | "k"
        | "kibana" -> Some Kibana
        | _ -> None

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
        
let (|IsProduct|_|) candidate = Product.tryParse candidate
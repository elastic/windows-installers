#I "../../packages/build/FAKE.x64/tools"

#r "FakeLib.dll"
#load "Paths.fsx"

open System.Globalization
open Fake

/// The type of distribution package
type Distribution =
    | Msi
    | Zip
    
    member this.Extension =
        match this with
        | Msi -> "msi"
        | Zip -> "zip"
        
    member this.Name =
        match this with
        | Msi -> "Msi"
        | Zip -> "Zip"
        
    static member parse candidate =
        match candidate |> toLower with
        | "msi" -> Msi
        | "zip" -> Zip
        | _ -> failwithf "Cannot parse distribution from: %s" candidate
        
let (|IsDistribution|_|) candidate =
    match candidate |> toLower with
    | "msi" -> Some Msi
    | "zip" -> Some Zip
    | _ -> None

/// Source of an artifact
type Source = 
    | Official
    | Staging
    | Snapshot
    
    member this.Name =
        match this with
        | Official -> "Official"
        | Staging -> "Staging"
        | Snapshot -> "Snapshot"
    
    member this.Display =
        match this with
        | Official -> "Official release"
        | Staging -> "Staging Build candidate for official release"
        | Snapshot -> "Snapshot On demand or nightly build"
    
let (|IsSource|_|) candidate =
    match candidate |> toLower with
    | "official" -> Some Official
    | "staging" -> Some Staging
    | "snapshot" -> Some Snapshot
    | _ -> None
  
/// The Elastic Stack product 
type Product =
    | Elasticsearch
    | Kibana
    
    static member parse candidate =
        match candidate |> toLower with
        | "e"
        | "es"
        | "elasticsearch" -> Elasticsearch
        | "k"
        | "kibana" -> Kibana
        | _ -> failwithf "Cannot parse product from: %s" candidate
        
    static member tryParse candidate =
        match candidate |> toLower with
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
        
    member this.Title = CultureInfo.InvariantCulture.TextInfo.ToTitleCase this.Name
        
let (|IsProduct|_|) candidate = Product.tryParse candidate
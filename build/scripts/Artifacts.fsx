#I "../../packages/build/FAKE.x64/tools"
#I "../../packages/build/Fsharp.Data/lib/net40"
#I "../../packages/build/FSharp.Text.RegexProvider/lib/net40"

#r "FakeLib.dll"
#r "FSharp.Data.dll"
#r "Fsharp.Text.RegexProvider.dll"
#r "System.Xml.Linq.dll"

#load "Paths.fsx"
#load "Products.fsx"
#load "Versions.fsx"
#load "Feeds.fsx"

open System.Collections.Generic
open System.IO
open System.Text.RegularExpressions
open Fake.FileHelper
open Fake
open Feeds
open Products
open Paths.Paths
open Versions


/// An artifact requested by input
type RequestedArtifact =
        { Product : Product
          Version : RequestedVersion
          Distribution : Distribution
          Source : Source } with
        
    static member create product version distribution source =
        { Product = product;
          Version = version;
          Distribution = distribution;
          Source = source }
        
    static member LatestElasticsearch = RequestedArtifact.create Elasticsearch Latest Zip Official
    
    /// Tries to parse a requested artifact from a string of the form
    /// <product>:[version, branch, buildid]:[distribution]:[source]
    ///
    /// Examples
    /// es:6.6.0:zip:official = Official release of Elasticsearch 6.6.0 zip
    /// es:6.x:msi:staging    = Latest staging release Elasticsearch 6.x MSI  
    /// es:6:zip              = Latest official release of Elasticsearch 6.x zip
    /// es:6:snapshot         = Latest snapshot release of Elasticsearch 6.x zip 
    /// es:6                  = Latest official release of Elasticsearch 6.x zip
    /// es                    = Latest official release of Elasticsearch zip
    static member tryParse candidate =
        match candidate |> split ':' with
        | [ IsProduct p; IsRequestedVersion v; IsDistribution d; IsSource s ] -> Some (RequestedArtifact.create p v d s)
        | [ IsProduct p; IsRequestedVersion v; IsSource s; ] -> Some (RequestedArtifact.create p v Zip s)
        | [ IsProduct p; IsRequestedVersion v; IsDistribution d; ] -> Some (RequestedArtifact.create p v d Official)
        | [ IsProduct p; IsRequestedVersion v; ] -> Some (RequestedArtifact.create p v Zip Official)
        | [ IsProduct p; IsDistribution d; IsSource s; ] -> Some (RequestedArtifact.create p Latest d s)
        | [ IsProduct p; IsSource s; ] -> Some (RequestedArtifact.create p Latest Zip s)
        | [ IsProduct p; IsDistribution d; ] -> Some (RequestedArtifact.create p Latest d Official)
        | [ IsProduct p; ] -> Some (RequestedArtifact.create p Latest Zip Official)
        | _ -> None
    
    /// Creates a requested artifact from a single zip file in a directory
    static member fromDir (product:Product) dir =
        let extractVersion (fileInfo:FileInfo) =
            Regex.Replace(fileInfo.Name, "^" + product.Name + "\-(.*?)(?:\-windows\-x86_64)?\.zip$", "$1")
        
        let zips = InDir |> directoryInfo |> filesInDirMatching (product.Name + "*.zip")
        match zips.Length with
        | 0 -> failwithf "No %s zip file found in %s" product.Name InDir
        | 1 ->
            let version = zips.[0] |> extractVersion |> Version.parse
            tracefn "Extracted %s from %s" version.FullVersion zips.[0].FullName          
            { Product = product
              Version = Version version
              Distribution = Zip
              Source = Official }
        | _ -> failwithf "Expecting one %s zip file in %s but found %i" product.Name InDir zips.Length
          
    member this.tryFindDownloadedArtifact =
        match this.Source with
        | Official ->
            match this.Version with
            | Version v when v.IsSnapshot = false && v.BuildId = "" ->
                let filename = InDir @@ sprintf "%s-%s.%s" this.Product.Name (this.Version.ToString()) this.Distribution.Extension
                match filename |> fileExists with
                | true ->
                    Some { Title = this.Product.Title
                           Product = this.Product
                           Version = v
                           Distribution = this.Distribution
                           Description = None
                           PubDate = None
                           Url = None
                           DownloadPath = Some filename }
                | false -> None
            | _ -> None
        | Snapshot
        | Staging ->
            // Can only be certain that one staging or snapshot build is the same as another when there is a build id
            let tryFindDownloadArtifact versionOrBuildId =
                match Directory.GetFiles(InDir, sprintf "%s*%s.%s" this.Product.Name versionOrBuildId this.Distribution.Extension) with
                 | [| path |] ->
                    let package = ArtifactsFeed.parsePackage <| Path.GetFileName path                
                    Some { Title = this.Product.Title
                           Product = this.Product
                           Version = package.Version
                           Distribution = this.Distribution
                           Description = None
                           PubDate = None
                           Url = None
                           DownloadPath = Some path }
                 | _ -> None 
                    
            match this.Version with            
            | BuildId buildId -> tryFindDownloadArtifact buildId             
            | Version v -> tryFindDownloadArtifact <| v.ToString()   
            | _ -> None
            
/// Determines if str is a requested artifact                                         
let (|IsRequestedArtifact|_|) str = RequestedArtifact.tryParse str

/// Determines if str is a collection of requested artifacts 
let (|IsRequestedArtifactList|_|) str =
    let artifactStrings = str |> split ','
    let artifacts = new List<RequestedArtifact>()
    
    artifactStrings
    |> List.iter (fun s ->
        match s with
        | IsRequestedArtifact requestedArtifact -> artifacts.Add(requestedArtifact)
        | _ -> ())
    
    match artifacts with
    | v when v.Count = artifactStrings.Length -> Some (List.ofSeq v)
    | _ -> None
   
/// A resolved artifact
type ResolvedArtifact(requested:RequestedArtifact, resolved:Artifact) =

    member this.Product = resolved.Product
    
    member this.Version = resolved.Version
    
    member this.Distribution = resolved.Distribution
    
    member this.Source = requested.Source
    
    member this.DownloadUrl =
        match resolved.Url with
        | Some s -> s
        | None -> ""
                
    member this.DownloadPath =
        match resolved.DownloadPath with
        | Some p -> p // Download path was resolved from file that exists in disk
        | None ->
            match requested.Source with
            | Official ->  InDir @@ Path.GetFileName this.DownloadUrl
            | Snapshot
            | Staging -> InDir @@ resolved.Version.ToString() @@ Path.GetFileName this.DownloadUrl
                  
    member this.Download () =
        match (this.DownloadUrl, this.DownloadPath) with
        | (_, file) when fileExists file ->
            tracefn "Already downloaded %s to %s" resolved.Product.Name file
        | (url, file) ->
            tracefn "Downloading %s from %s" this.Product.Name url 
            let targetDirectory = file |> Path.GetDirectoryName
            if (directoryExists targetDirectory |> not) then CreateDir targetDirectory
            use webClient = new System.Net.WebClient()
            (url, file) |> webClient.DownloadFile
            tracefn "Done downloading %s from %s to %s" this.Product.Name url file
            
        match resolved.Distribution with
        | Zip ->
            
            let extractedTargetDirectory = this.ExtractedDirectory |> Path.GetDirectoryName

            tracefn "extracted directory: %s" this.ExtractedDirectory
            
            if not <| directoryExists this.ExtractedDirectory then
                tracefn "Unzipping %s to %s" this.Product.Name extractedTargetDirectory
                Unzip extractedTargetDirectory this.DownloadPath
                
                // TODO: This may no longer be needed...
                // Rename kibana unzip
                match this.Product with
                    | Kibana ->
                        let original = sprintf "kibana-%s-windows-x86" this.Version.FullVersion
                        if directoryExists original |> not then
                            Rename (extractedTargetDirectory @@ (sprintf "kibana-%s" this.Version.FullVersion)) (extractedTargetDirectory @@ original)
                    | _ -> ()
            else tracefn "Extracted directory %s already exists" this.ExtractedDirectory   
        | _ -> ()
        
    member this.ExtractedDirectory =       
        let fileName =
            let suffix = DownloadSuffix this.Product this.Version
            let name = this.DownloadPath |> Path.GetFileNameWithoutExtension
            if suffix.Length > 0 then name.Substring(0, name.Length - suffix.Length)
            else name
            
        (this.DownloadPath |> Path.GetDirectoryName) @@ fileName
            
    member this.ServiceDir = ProcessHostsDir @@ sprintf "Elastic.ProcessHosts.%s/" this.Product.Title

    member this.ServiceBinDir = this.ServiceDir @@ "bin/AnyCPU/Release/"
    
    member this.BinDir = this.ExtractedDirectory @@ "bin/"

    member this.OutMsiDir = OutDir @@ this.Product.Name @@ this.Version.ToString()
    
    member this.OutMsiPath = this.OutMsiDir @@ sprintf "%s-%s.msi" this.Product.Name this.Version.FullVersion
        
    static member IsZip (resolvedArtifact:ResolvedArtifact) = resolvedArtifact.Distribution = Zip
            
let private findInOfficialFeed (requested: RequestedArtifact) =
    match requested.Source with
    | Snapshot
    | Staging -> None
    | Official ->
        match requested.Version with
        | Latest ->
            let latest = ReleasesFeed.GetLatest requested.Product
            Some (latest.getArtifact requested.Distribution)
        | Branch b ->
            let maybeRelease filter =
               ReleasesFeed.GetReleases ()
               |> Array.filter (fun r -> (filter r) && r.Product = requested.Product)
               |> Array.tryHead 
            
            match b.Minor with
            | Number n ->
                match maybeRelease (fun r -> r.Version.Major = b.Major && r.Version.Minor = n) with
                | Some r -> Some (r.getArtifact requested.Distribution)
                | None -> None
            | X ->
                match maybeRelease (fun r -> r.Version.Major = b.Major) with
                | Some r -> Some (r.getArtifact requested.Distribution)
                | None -> None
        | Version v ->
            let maybeRelease =
                ReleasesFeed.GetReleases ()
                |> Array.filter (fun r -> r.Version = v && r.Product = requested.Product)
                |> Array.tryHead
                
            match maybeRelease with
            | Some r -> Some (r.getArtifact requested.Distribution)
            | None -> None
        | BuildId buildId -> None 

let private findInArtifactsFeed (requested : RequestedArtifact) =              
    match requested.Source with
    | Official -> None
    | Snapshot
    | Staging ->
        match requested.Version with
        | Latest ->         
            let feed = if requested.Source = Snapshot then ArtifactsFeed.GetLatestSnapshot
                       else ArtifactsFeed.GetLatestStaging           
            Some (feed requested.Product requested.Distribution)
        | Branch b -> ArtifactsFeed.Search requested.Product requested.Version requested.Distribution
        | Version v -> ArtifactsFeed.Search requested.Product requested.Version requested.Distribution
        | BuildId buildId -> ArtifactsFeed.GetByBuildId requested.Product requested.Distribution buildId

let findInFeeds (requested:RequestedArtifact) =
    // skip even looking in the feed if we've already downloaded
    match requested.tryFindDownloadedArtifact with
    | Some a -> Some (new ResolvedArtifact(requested, a))
    | None ->
        let maybeArtifact =
            match requested.Source with
            | Official -> findInOfficialFeed requested
            | Staging
            | Snapshot -> findInArtifactsFeed requested
        match maybeArtifact with
        | Some a -> Some (new ResolvedArtifact(requested, a))
        | None -> None
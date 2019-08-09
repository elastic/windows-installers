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
open Fake.FileHelper
open Fake
open Feeds
open Paths
open Products
open Versions

/// how the request input is received
type RequestedInput =
    /// A requested version input
    | Value of string
    /// A path to a file within build/in
    | Path of string

/// An artifact requested by input
type RequestedArtifact =
        { Product: Product
          Version: RequestedVersion
          Distribution: Distribution
          Source: Source
          /// The original input for the requested artifact
          Input: RequestedInput } with
        
    static member create product version distribution source rawInput =
        { Product = product;
          Version = version;
          Distribution = distribution;
          Source = source
          Input = rawInput }
        
    static member LatestElasticsearch = RequestedArtifact.create Elasticsearch Latest Zip Official (Value "x")
    
    /// Tries to parse a requested artifact from a string of the form
    /// <product>:[version, branch, buildid, x]:[distribution]:[source]
    ///
    /// Examples
    /// es:6.6.0:zip:official = Official release of Elasticsearch 6.6.0 zip
    /// es:6.x:msi:staging    = Latest staging release Elasticsearch 6.x MSI  
    /// es:6:zip              = Latest official release of Elasticsearch 6.x zip
    /// es:6:snapshot         = Latest snapshot release of Elasticsearch 6.x zip 
    /// es:6                  = Latest official release of Elasticsearch 6.x zip
    /// es:x:staging          = Latest staging release of Elasticsearch zip
    /// es                    = Latest official release of Elasticsearch zip
    /// es:msi                = Latest official release of Elasticsearch MSI
    /// es:snapshot           = Latest snapshot release of Elasticsearch zip
    /// es:msi:snapshot       = Latest snapshot release of Elasticsearch MSI
    static member tryParse candidate =
        match candidate |> split ':' with
        | [ IsProduct p; IsRequestedVersion v; IsDistribution d; IsSource s ] -> Some (RequestedArtifact.create p v d s (Value candidate))
        | [ IsProduct p; IsRequestedVersion v; IsDistribution d; ] ->
            match v with
            | Version vv ->
                match (vv.BuildId, vv.IsSnapshot) with
                | ("", false) -> Some (RequestedArtifact.create p v d Official (Value candidate))
                | ("", true) -> Some (RequestedArtifact.create p v d Snapshot (Value candidate))
                | (_, false) -> Some (RequestedArtifact.create p v d Staging (Value candidate))
                | (_, true) -> Some (RequestedArtifact.create p v d Snapshot (Value candidate))
            | BuildId buildId -> Some (RequestedArtifact.create p v d Snapshot (Value candidate))
            | Latest -> Some (RequestedArtifact.create p v d Official (Value candidate))
            | Branch _ -> Some (RequestedArtifact.create p v d Official (Value candidate))
        | [ IsProduct p; IsRequestedVersion v; IsSource s; ] -> Some (RequestedArtifact.create p v Zip s (Value candidate))
        | [ IsProduct p; IsRequestedVersion v; ] ->
            match v with
            | Version vv ->
                match (vv.BuildId, vv.IsSnapshot) with
                | ("", false) -> Some (RequestedArtifact.create p v Zip Official (Value candidate))
                | ("", true) -> Some (RequestedArtifact.create p v Zip Snapshot (Value candidate))
                | (_, false) -> Some (RequestedArtifact.create p v Zip Staging (Value candidate))
                | (_, true) -> Some (RequestedArtifact.create p v Zip Snapshot (Value candidate))
            | BuildId buildId -> Some (RequestedArtifact.create p v Zip Snapshot (Value candidate))
            | Latest -> Some (RequestedArtifact.create p v Zip Official (Value candidate))
            | Branch _ -> Some (RequestedArtifact.create p v Zip Official (Value candidate))
        | [ IsProduct p; IsDistribution d; IsSource s; ] -> Some (RequestedArtifact.create p Latest d s (Value candidate))
        | [ IsProduct p; IsSource s; ] -> Some (RequestedArtifact.create p Latest Zip s (Value candidate))
        | [ IsProduct p; IsDistribution d; ] -> Some (RequestedArtifact.create p Latest d Official (Value candidate))
        | [ IsProduct p; ] -> Some (RequestedArtifact.create p Latest Zip Official (Value candidate))
        | _ -> None
    
    /// Creates a requested artifact from a single zip file found by recursively searching a directory
    static member fromDir (product:Product) dir =
        let pattern = sprintf "%s*.zip" product.Name       
        let zips = dir |> directoryInfo |> filesInDirMatchingRecursive pattern
        match zips with
        | [| |] -> failwithf "No %s zip file found in %s matching %s" product.Name dir pattern
        | [| zipFile |] ->
            // infer the build id and source from the parent directory
            let (buildId, sourceFromInDir) =
                match zipFile.Directory.Name |> toLower with
                | "in" -> "", Official
                | dirName ->
                    let version = dirName |> Version.parse
                    if version.IsSnapshot then version.BuildId, Snapshot
                    else version.BuildId, Staging
            
            // augment the version from the zip file with the build id from the parent directory        
            let version =
                let package = zipFile.Name |> ArtifactsFeed.parsePackage
                { package.Version with BuildId = buildId }
                
            let source =
                match version.BuildId with
                | "" ->
                    // for a release build, it may be in build/in directly. We can only differentiate between
                    // snapshot and not snapshot here, since there is no other distinguishing identifer available
                    // to distringuish staging from official. In reality, the artifact has been resolved from disk,
                    // so this difference only affects the outputted Source string in the build output. 
                    if version.IsSnapshot = true  then Snapshot
                    else sourceFromInDir
                | _ -> sourceFromInDir
                    
            tracefn "Extracted version %s %s from %s" version.FullVersion version.BuildId zipFile.FullName          
            { Product = product
              Version = Version version
              Distribution = Zip
              Source = source
              Input = Path zipFile.FullName }
        | _ ->
            failwithf "Expecting one %s zip file in %s but found %i" product.Name dir zips.Length
          
    /// Attempts to find an already downloaded artifact matching the requested version
    member this.tryFindDownloadedArtifact =
        match this.Input with
        | Path path ->
            match this.Version with
            | Version v ->
                Some { Title = this.Product.Title
                       Product = this.Product
                       Version = v
                       Distribution = this.Distribution
                       Description = None
                       PubDate = None
                       Url = None
                       DownloadPath = Some path }
            | _ -> None
        | Value _ ->      
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
                let tryFindDownloadArtifactInDir pattern buildId =
                    match Directory.GetDirectories(InDir, pattern) with
                    | [| versionDir |] ->
                        match Directory.GetFiles(versionDir, sprintf "%s*.%s" this.Product.Name this.Distribution.Extension) with
                        | [| path |] ->
                           let package = ArtifactsFeed.parsePackage <| Path.GetFileName path                
                           Some { Title = this.Product.Title
                                  Product = this.Product
                                  Version = { package.Version with BuildId = buildId }
                                  Distribution = this.Distribution
                                  Description = None
                                  PubDate = None
                                  Url = None
                                  DownloadPath = Some path }
                        | _ -> None
                    | _ -> None
                    
                // Can only be certain that one staging or snapshot build is the same as another when there is a build id present   
                match this.Version with            
                | BuildId buildId -> tryFindDownloadArtifactInDir (sprintf "*%s" buildId) buildId                        
                | Version v ->           
                    match v.BuildId with
                    | "" -> None
                    | buildId -> tryFindDownloadArtifactInDir (v.ToString()) buildId
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

    member this.RequestedInput =
        match requested.Input with
        | Path p -> sprintf "file at %s" p
        | Value v -> v
    
    member this.Product = resolved.Product
    
    member this.Version = resolved.Version
    
    member this.Distribution = resolved.Distribution
    
    member this.Source = requested.Source
    
    /// Gets the identifier for this resolved artifact
    member this.Identifier =
        match this.Version.BuildId with
        | "" ->  sprintf "%s:%s:%s:%s" this.Product.Name this.Version.FullVersion this.Source.Name this.Distribution.Name
        | buildId ->  sprintf "%s:%s:%s:%s:%s" this.Product.Name this.Version.FullVersion this.Source.Name this.Distribution.Name buildId
    
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
            tracefn "Extracted directory: %s" this.ExtractedDirectory
            
            if not <| directoryExists this.ExtractedDirectory then
                tracefn "Unzipping %s to %s" this.Product.Name extractedTargetDirectory
                Unzip extractedTargetDirectory this.DownloadPath
                
                // TODO: This may no longer be needed. Check once Kibana MSIs are built
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
            let suffix = DownloadSuffix this.Product this.Version this.Distribution
            let name = this.DownloadPath |> Path.GetFileNameWithoutExtension
            if suffix.Length > 0 && name.EndsWith(suffix) then name.Substring(0, name.Length - suffix.Length)
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
               |> Array.filter (fun r -> r.Product = requested.Product && filter r)
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
        | Version v ->
            // without a build id, get the latest build for the requested version
            if v.BuildId = "" then ArtifactsFeed.Search requested.Product requested.Version requested.Distribution
            else ArtifactsFeed.GetByVersion requested.Product requested.Distribution v
        | BuildId buildId -> ArtifactsFeed.GetByBuildId requested.Product requested.Distribution buildId

/// Attempts to find the requested artifact in the build/in directory. If it cannot be found,
/// then it will attempt to be downloaded from the Official releases feed or Artifacts API
let findDownloadedOrInFeeds (requested:RequestedArtifact) =
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
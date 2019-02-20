#I "../../packages/build/FAKE.x64/tools"
#I @"../../packages/build/Fsharp.Data/lib/net40"
#I @"../../packages/build/FSharp.Text.RegexProvider/lib/net40"

#r "FakeLib.dll"
#r "FSharp.Data.dll"
#r "Fsharp.Text.RegexProvider.dll"
#r "System.Xml.Linq.dll"

#load "Products.fsx"
#load "Versions.fsx"

open System
open System.Net
open FSharp.Data
open FSharp.Text.RegexProvider
open Fake
open Products
open Versions

ServicePointManager.SecurityProtocol <- SecurityProtocolType.Ssl3 |||
                                        SecurityProtocolType.Tls |||
                                        SecurityProtocolType.Tls11 |||
                                        SecurityProtocolType.Tls12

let DownloadSuffix (product:Product) (version:Version) =
    match product with
    | Elasticsearch ->
        match version.Major with
            | v when v >= 7 -> "-windows-x86_64"
            | _ -> ""
    | _ -> ""

/// An artifact from a feed with which to build an MSI or run integration tests
type Artifact =
    { Title: string
      Product: Product
      Version: Version
      Distribution: Distribution
      Description: string option
      PubDate: DateTime option
      Url: string option
      DownloadPath: string option }
                                        
/// Artifacts API for retrieving Staging and Snapshot builds                                 
module ArtifactsFeed =
   
    let private artifactsUrl = "https://artifacts-api.elastic.co/v1"
    let private artifactVersionsUrl = sprintf "%s/versions" artifactsUrl  
    let private artifactVersionBuildsUrl version = sprintf "%s/%s/builds" artifactVersionsUrl version    
    let private artifactVersionBuildUrl version build = sprintf "%s/%s/builds/%s" artifactVersionsUrl version build
    let private artifactSearchUrl branchOrVersion filters = sprintf "%s/search/%s/%s" artifactsUrl branchOrVersion filters

    let private downloadJson (url:string) =
        use webClient = new WebClient()
        webClient.DownloadString url |> JsonValue.Parse
     
    let private props (jsonValue:JsonValue) = jsonValue.Properties()      
    let private prop name (jsonValue:JsonValue) = jsonValue.GetProperty name
    let private tryProp name (jsonValue:JsonValue) = jsonValue.TryGetProperty name
    let private toArray (jsonValue:JsonValue) = jsonValue.AsArray()
    let private toString (jsonValue:JsonValue) = jsonValue.AsString()
    let private toVersion (jsonValue:JsonValue) = jsonValue |> toString |> Version.parse
    let private innerText (jsonValue:JsonValue) = jsonValue.InnerText()
        
    /// Gets snapshot and staging versions available
    let GetVersions () =
        artifactVersionsUrl
        |> downloadJson
        |> prop "versions"
        |> toArray
        |> Seq.rev
        |> Seq.map toVersion

    /// Gets the builds associated with a snapshot or staging version
    /// 
    /// https://artifacts-api.elastic.co/v1/versions/{version}/builds/ takes {version} of the form
    /// 
    /// - major.minor
    /// - major.minor.patch
    /// - major.minor-<prerelease or SNAPSHOT>
    /// - major.minor.patch-<prerelease or SNAPSHOT>
    let GetBuilds (version:Version) =
        artifactVersionBuildsUrl version.FullVersion
        |> downloadJson
        |> prop "builds"
        |> toArray
        |> Seq.map (fun v ->
            let buildVersion = toVersion v
            // copy over the build id from this build version, and retain details of the input version.
            // For example, 7.0.0-SNAPSHOT might resolve to build 7.0.0-c178cc88, so we want to retain the -SNAPSHOT suffix
            { version with BuildId = buildVersion.BuildId })
          
    type Package =
        { // cannot use Product type as Artifacts API contains products that are not modelled
          Product: string
          Version: Version
          Distribution: Distribution }
        
    type private PackageRegex = Regex< @"^(?<Product>.*?)?-(?<Version>(?<Major>\d+)\.(?<Minor>\d+)\.(?<Patch>\d+)(?:\-(?<Prerelease>[\w\-]+?))?)\.(?<Distribution>.*)$", noMethodPrefix=true >
    let private packageRegex = new PackageRegex()
        
    let parsePackage name =
        let m = packageRegex.Match name
        if m.Success = false then failwithf "failed to parse package name from: %s" name
        {
            Product = m.Product.Value
            Version = Version.parse m.Version.Value
            Distribution = Distribution.parse m.Distribution.Value
        }
        
    /// Gets the artifact for a given product version and distribution
    let GetArtifact (product:Product) (distribution:Distribution) version =
        let artifactName (product:Product) (version:Version) (distribution:Distribution) =
            let suffix = DownloadSuffix product version
            sprintf "%s-%s%s.%s" product.Name version.FullVersion suffix distribution.Extension
      
        let buildProp = artifactVersionBuildUrl version.FullVersion version.BuildId
                        |> downloadJson
                        |> prop "build"
        
        let pubDate = buildProp |> prop "end_time" |> toString |> DateTime.Parse
        
        let url = buildProp
                    |> prop "projects"
                    |> prop product.Name
                    |> prop "packages"
                    |> prop (artifactName product version distribution)
                    |> prop "url"
                    |> innerText
       
        { Title = product.Title
          Product = product
          Version = version
          Distribution = distribution
          Description = None
          PubDate = Some pubDate
          Url = Some url
          DownloadPath = None }
        
    /// Searches for artifacts for a particular branch or version.
    /// Searching by latest
    let Search (product:Product) (requested:RequestedVersion) (distribution:Distribution) =
        match requested with
        | BuildId id -> None
        | Latest -> None
        | _ ->      
            let filters = sprintf "%s,%s" product.Name distribution.Extension
            let branchOrVersion = requested.ToString ()
            let json = artifactSearchUrl branchOrVersion filters |> downloadJson
    
            match json |> tryProp "error-message" with
            | Some message ->
                message |> toString |> sprintf "failed to find %s in artifact search: %s" branchOrVersion  |> traceImportant
                None
            | None ->              
                let packages = json |> prop "packages" |> props
                match packages with
                | [| |] -> None
                | _ ->
                    let (package, url) =
                        packages
                        |> Array.map (fun (name, value) -> name |> parsePackage, value |> prop "url" |> innerText)
                        |> Array.filter (fun (package, url) -> package.Product = product.Name)
                        |> Array.head
                        
                    let BuildVersion = (new Uri(url)).Segments.[1].TrimEnd('/') |> Version.parse
                
                    Some { Title = product.Title
                           Product = product
                           Version = { package.Version with BuildId = BuildVersion.BuildId }
                           Distribution = distribution
                           Description = None
                           PubDate = None
                           Url = Some url
                           DownloadPath = None }
    
    let GetByBuildId (product:Product) (distribution:Distribution) buildId =
        match buildId with
        | IsBuildId id ->
            let version = GetVersions ()
                          |> Seq.map GetBuilds
                          |> Seq.concat
                          |> Seq.filter (fun v -> v.BuildId = buildId)
                          |> Seq.tryHead
             
            match version with
            | Some v ->
                Some (GetArtifact product distribution v)
            | None -> None
        | _ -> None
                       
    /// Gets the latest artifact for a given product and distribution filtered by version
    let GetLatest (product:Product) (distribution:Distribution) versionFilter =
        GetVersions()
        |> Seq.filter versionFilter
        |> Seq.head
        |> GetBuilds
        |> Seq.head
        |> GetArtifact product distribution
        
    /// Gets the latest staging artifact for a given product and distribution
    let GetLatestStaging (product:Product) (distribution:Distribution) =
        GetLatest product distribution (fun v -> v.IsSnapshot = false)
        
    /// Gets the latest staging artifact for a given product and distribution
    let GetLatestSnapshot (product:Product) (distribution:Distribution) =
        GetLatest product distribution (fun v -> v.IsSnapshot = true)
    
    /// Gets the latest artifact for a major version of a given product and distribution
    let GetLatestInMajor (product:Product) (distribution:Distribution) major =
        GetLatest product distribution (fun v -> v.Major = major)
          
/// Past official releases feed   
module ReleasesFeed =
    
    let private pastReleasesDownloadUrl = "https://artifacts.elastic.co/downloads"
    
    [<Literal>]
    let private feedUrl = "https://www.elastic.co/downloads/past-releases/feed"

    type private ReleasesFeedXml = XmlProvider< "feed-example.xml" >
    
    let buildDownloadUrl (product:Product) (version:Version) (distribution:Distribution) =
        let suffix = DownloadSuffix product version
        sprintf "%s/%s/%s-%s%s.%s" pastReleasesDownloadUrl product.Name
            product.Name version.FullVersion suffix distribution.Extension
    
    type Release =
        { Title: string
          Product: Product
          Version: Version
          Description: string option
          PubDate: DateTime option
          Url: string }
        
        /// Gets an artifact from a release
        member this.getArtifact distribution =
            { Title = this.Title
              Product = this.Product
              Version = this.Version
              Distribution = distribution
              Description = this.Description
              PubDate = this.PubDate
              Url = Some (buildDownloadUrl this.Product this.Version distribution)
              DownloadPath = None }
        
    type private ProductVersionRegex = Regex< @"^(?:\s*(?<Product>.*?)\s*)?(?<Version>(?<Major>\d+)\.(?<Minor>\d+)\.(?<Patch>\d+)(?:\-(?<Prerelease>[\w\-]+))?)", noMethodPrefix=true >
    
    let private parseProductAndVersion title =
        match ProductVersionRegex().Match title with
        | m when m.Success = true ->
            // This effectively filters out products not in Product
            match Product.tryParse m.Product.Value with
            | Some p -> Some (p, Version.parse m.Version.Value)
            | None -> None
        | _ -> None
    
    /// Gets the past releases from the Releases feed
    let GetReleases () =      
        let feed = ReleasesFeedXml.Load feedUrl
        feed.Channel.Items
        |> Array.map (fun i ->
            match i.Title |> toLower |> parseProductAndVersion with
            | Some (product, version) ->
                Some { Title = i.Title
                       Product = product
                       Version = version
                       Description = i.Description
                       PubDate = i.PubDate
                       Url = i.Link }
            | None -> None)
        |> Array.choose id
        
    /// Gets the latest release
    let GetLatest (product:Product) =
        GetReleases()
        |> Array.filter (fun p -> p.Product = product)
        |> Array.sortByDescending (fun p -> p.Version)
        |> Array.head
#I @"../../packages/build/FAKE/tools"
#I @"../../packages/build/Fsharp.Data/lib/net40"
#I @"../../packages/build/FSharp.Text.RegexProvider/lib/net40"

#r @"FakeLib.dll"
#r "Fsharp.Data.dll"
#r "Fsharp.Text.RegexProvider.dll"
#r "System.Xml.Linq.dll"
#load "Products.fsx"

open System
open System.IO
open System.Text.RegularExpressions
open Fake
open FSharp.Data
open FSharp.Text.RegexProvider
open Products.Products
open Products.Paths

module Commandline =

    let private usage = """
USAGE:

build <target> [products] [version] [params] [skiptests]

Targets:

* buildinstallers
  - default target if non provided. Builds installers for products
* buildservices
  - Builds services for products
* clean
  - cleans build output folders
* unittest
  - build and unit test
* downloadproducts
  - downloads the product zip files if not already downloaded
* unzipproducts
  - unzips product zip files if not already unzipped
* release [products] [version] [certFile] [passwordFile]
  - create a release versions of each MSI by building and then signing the service executable and installer for each.
  - when certFile and passwordFile are specified, these will be used for signing otherwise the values in ELASTIC_CERT_FILE
    and ELASTIC_CERT_PASSWORD will be used
* integrate [products] [version] [testtargets] [skiptests]  -
  - run integration tests. Can filter tests by wildcard [testtargets]
* help or ?
  - show this usage summary

Products:

optional comma separated collection of products to build. can use

* a
* all
    - build all products
* e
* es
* elasticsearch
    - build elasticsearch
* k
* kibana
    - build kibana

Version:

optional version to build. When specified, for build targets other than release, the version zip file will
be downloaded and extracted to build/in directory if it doesn't already exist.

when not specified
    - for build targets other than release, the latest non-prelease version of each product will be downloaded
    - for release, the build/in directory will be checked and a single version found there will be used
"""

    [<Literal>]
    let private feedUrl = "https://www.elastic.co/downloads/past-releases/feed"

    type DownloadFeed = XmlProvider< feedUrl >

    type VersionRegex = Regex< @"^(?:\s*(?<Product>.*?)\s*)?(?<Version>(?<Major>\d+)\.(?<Minor>\d+)\.(?<Patch>\d+)(?:\-(?<Prerelease>[\w\-]+))?)$", noMethodPrefix=true >

    let private parseVersion version =
        let m = VersionRegex().Match version
        if m.Success |> not then failwithf "Could not parse version from %s" version
        { Product = m.Product.Value;
          FullVersion = m.Version.Value;
          Major = m.Major.Value |> int;
          Minor = m.Minor.Value |> int;
          Patch = m.Patch.Value |> int;
          Prerelease = m.Prerelease.Value; }

    let private lastFeedVersion (product : Product) =
        // TODO: disallow prereleases for moment. Make build parameter in future?
        let itemIsProduct itemText =
            let m = parseVersion itemText
            m.Product = product.Title && (isNullOrWhiteSpace m.Prerelease)
        tracefn "Loading download feed data from %s" feedUrl
        let feed = DownloadFeed.Load feedUrl
        let firstLink = feed.Channel.Items |> Seq.find (fun item -> itemIsProduct item.Title)
        let version = parseVersion firstLink.Title
        tracefn "Extracted %s version %s from '%s'" product.Name version.FullVersion firstLink.Title
        version

    let private versionFromInDir (product : Product) =
        let extractVersion (fileInfo:FileInfo) =
            Regex.Replace(fileInfo.Name, "^" + product.Name + "\-(.*)\.zip$", "$1")
        let zips = InDir
                   |> directoryInfo
                   |> filesInDirMatching (product.Name + "*.zip")
        match zips.Length with
        | 0 -> failwithf "No %s zip file found in %s" product.Name InDir
        | 1 ->
            let version = zips.[0] |> extractVersion |> parseVersion
            tracefn "Extracted %s from %s" version.FullVersion zips.[0].FullName
            version
        | _ -> failwithf "Expecting one %s zip file in %s but found %i" product.Name InDir zips.Length


    let private args = getBuildParamOrDefault "cmdline" "buildinstallers" |> split ' '
    let private skipTests = args |> List.exists (fun x -> x = "skiptests")
    let private filteredArgs = args |> List.filter (fun x -> x <> "skiptests")

    let target =
        match (filteredArgs |> List.tryHead) with
        | Some t -> t
        | _ -> "buildinstallers"

    let arguments =
        match filteredArgs with
        | _ :: tail -> target :: tail
        | [] -> [target]

    let private (|IsTarget|_|) (candidate: string) =
        match candidate.ToLowerInvariant() with
        | "buildservices"
        | "buildinstallers"
        | "test"
        | "clean"
        | "downloadproducts"
        | "unzipproducts"
        | "patchguids"
        | "unittest"
        | "prunefiles"
        | "release"
        | "integrate" -> Some candidate
        | _ -> None

    let private (|IsVersion|_|) candidate =
        let m = VersionRegex().Match candidate
        match m.Success with
        | true -> Some {  Product = m.Product.Value;
                          FullVersion = m.Version.Value;
                          Major = m.Major.Value |> int;
                          Minor = m.Minor.Value |> int;
                          Patch = m.Patch.Value |> int;
                          Prerelease = m.Prerelease.Value; }
        | _ -> None

    let private (|IsProductList|_|) candidate =
        let products = splitStr "," candidate
        let productFromValue value =
            match value with
            | "all"
            | "a" -> All
            | "e"
            | "es"
            | "elasticsearch" -> [Elasticsearch]
            | "k"
            | "kibana" -> [Kibana]
            | _ -> []

        if products.Length <> 0 then
            products
            |> List.map productFromValue
            |> List.concat
            |> List.distinct
            |> Some
        else None

    let private certAndPasswordFromEnvVariables () =
        trace "getting signing cert and password from environment variables"
        [("ELASTIC_CERT_FILE", "certificate");("ELASTIC_CERT_PASSWORD", "password")]
        |> List.iter(fun (v, b) ->
                let ev = Environment.GetEnvironmentVariable(v, EnvironmentVariableTarget.Machine)
                if isNullOrWhiteSpace ev then failwithf "Expecting non-null value for %s environment variable" v
                setBuildParam b ev
           )

    let private certAndPasswordFromFile certFile passwordFile =
        trace "getting signing cert and password from file arguments"
        match (fileExists certFile, fileExists passwordFile) with
        | (true, true) ->
            setBuildParam "certificate" certFile
            passwordFile |> File.ReadAllText |> setBuildParam "password"
        | (false, _) -> failwithf "certificate file does not exist at %s" certFile
        | (_, false) -> failwithf "password file does not exist at %s" passwordFile

    let parse () =
        setEnvironVar "FAKEBUILD" "1"
        let products = match arguments with
                       | ["release"] ->
                           setBuildParam "release" "1"
                           certAndPasswordFromEnvVariables ()
                           All |> List.map (ProductVersion.CreateFromProduct versionFromInDir)
                       | ["release"; IsProductList products ] ->
                           setBuildParam "release" "1"
                           certAndPasswordFromEnvVariables ()
                           products |> List.map (ProductVersion.CreateFromProduct versionFromInDir)
                       | ["release"; IsVersion version ] ->
                           setBuildParam "release" "1"
                           certAndPasswordFromEnvVariables ()
                           All |> List.map (ProductVersion.CreateFromProduct <| fun _ -> version)
                       | ["release"; IsProductList products; IsVersion version ] ->
                           setBuildParam "release" "1"
                           certAndPasswordFromEnvVariables ()
                           products |> List.map (ProductVersion.CreateFromProduct <| fun _ -> version)
                       | ["release"; IsProductList products; IsVersion version; certFile; passwordFile ] ->
                           setBuildParam "release" "1"
                           certAndPasswordFromFile certFile passwordFile
                           products |> List.map (ProductVersion.CreateFromProduct <| fun _ -> version)
                       | ["release"; IsVersion version; certFile; passwordFile ] ->
                           setBuildParam "release" "1"
                           certAndPasswordFromFile certFile passwordFile
                           All |> List.map (ProductVersion.CreateFromProduct <| fun _ -> version)
                       | ["release"; IsProductList products; certFile; passwordFile ] ->
                           setBuildParam "release" "1"
                           certAndPasswordFromFile certFile passwordFile
                           products |> List.map (ProductVersion.CreateFromProduct versionFromInDir)
                       | ["release"; certFile; passwordFile ] ->
                           setBuildParam "release" "1"
                           certAndPasswordFromFile certFile passwordFile
                           All |> List.map (ProductVersion.CreateFromProduct versionFromInDir)

                       | ["integrate"; IsProductList products; IsVersion version; testTargets] ->
                           setBuildParam "testtargets" testTargets
                           products |> List.map (ProductVersion.CreateFromProduct <| fun _ -> version)
                       | ["integrate"; IsProductList products; IsVersion version] ->
                           products |> List.map (ProductVersion.CreateFromProduct <| fun _ -> version)
                       | ["integrate"; IsVersion version; testTargets] ->
                           setBuildParam "testtargets" testTargets
                           All |> List.map (ProductVersion.CreateFromProduct <| fun _ -> version)
                       | ["integrate"; IsProductList products; testTargets] ->
                           setBuildParam "testtargets" testTargets
                           products |> List.map (ProductVersion.CreateFromProduct lastFeedVersion)
                       | ["integrate"; IsProductList products] ->
                           products |> List.map (ProductVersion.CreateFromProduct lastFeedVersion)
                       | ["integrate"; IsVersion version] ->
                           All |> List.map (ProductVersion.CreateFromProduct <| fun _ -> version)
                       | ["integrate"; testTargets] ->
                           setBuildParam "testtargets" testTargets
                           All |> List.map (ProductVersion.CreateFromProduct lastFeedVersion)

                       | [IsTarget target; IsVersion version] ->
                           All |> List.map (ProductVersion.CreateFromProduct <| fun _ -> version)
                       | [IsTarget target; IsProductList products] ->
                           products |> List.map (ProductVersion.CreateFromProduct lastFeedVersion)
                       | [IsTarget target; IsProductList products; IsVersion version] ->
                           products |> List.map (ProductVersion.CreateFromProduct <| fun _ -> version)
                       | [IsTarget target] ->
                           All |> List.map (ProductVersion.CreateFromProduct lastFeedVersion)
                       | [] ->
                           All |> List.map (ProductVersion.CreateFromProduct lastFeedVersion)
                       | ["help"]
                       | ["?"] ->
                           trace usage
                           exit 2
                       | _ ->
                           traceError usage
                           exit 2

        setBuildParam "target" target
        if skipTests then setBuildParam "skiptests" "1"
        traceHeader target
        products

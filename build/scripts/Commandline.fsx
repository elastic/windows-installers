#I "../../packages/build/FAKE.x64/tools"
#I @"../../packages/build/Fsharp.Data/lib/net40"
#I @"../../packages/build/FSharp.Text.RegexProvider/lib/net40"

#r @"FakeLib.dll"
#r "Fsharp.Data.dll"
#r "Fsharp.Text.RegexProvider.dll"
#r "System.Xml.Linq.dll"
#load "Products.fsx"

open System
open System.Collections.Generic
open System.IO
open System.Text.RegularExpressions
open System.Net
open Fake
open FSharp.Data
open FSharp.Text.RegexProvider
open Products.Products
open Products.Paths

ServicePointManager.SecurityProtocol <- SecurityProtocolType.Ssl3 ||| SecurityProtocolType.Tls ||| SecurityProtocolType.Tls11 ||| SecurityProtocolType.Tls12;
ServicePointManager.ServerCertificateValidationCallback <- (fun _ _ _ _ -> true)

module Commandline =

    let usage = """
USAGE:

build.bat [Target] [Products] [Versions] [Target specific params] [skiptests]

Target:
-------

* buildinstallers
  - default target if none provided. Builds installers for products

* buildservices
  - Builds services for products

* clean
  - cleans build output folders

* patchguids
  - ensures a product GUID exists for the specified products and versions

* unittest
  - build and unit test

* downloadproducts
  - downloads the products if not already downloaded, and unzips them
    if not already unzipped

* release [Products] [Versions] [CertFile] [PasswordFile]
  - create a release versions of each MSI by building and then signing the service executable and installer for each.
  - when CertFile and PasswordFile are specified, these will be used for signing otherwise the values in ELASTIC_CERT_FILE
    and ELASTIC_CERT_PASSWORD environment variables will be used

  Example: build.bat release es 5.5.3 C:/path_to_cert_file C:/path_to_password_file

* integrate [Products] [Versions] [VagrantProvider] [TestTargets] [switches] [skiptests]  -
  - run integration tests. Can filter tests by wildcard [TestTargets], 
    which match against the directory names of tests

  Example: build.bat integrate es 5.5.1,5.5.2 local * skiptests

* help or ?
  - show this usage summary

Products:
---------

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

Versions:
---------

optional version(s) to build. Multiple versions can be specified, separated by commas. 

When specified, for build targets other than release, the product version zip files will
be downloaded and extracted to build/in directory if they don't already exist. 

A release version can be downloaded for integration tests by prefixing the version with r: e.g. r:5.5.2
A build candidate version can be downloaded for integration tests by prefixing the version with [buildhash]: e.g. e824d65e:5.6.0

when not specified
    - for build targets other than release, the latest non-prelease version of each product will be downloaded
    - for release, the build/in directory will be checked and a single version found there will be used

TestTargets:
------------

Wildcard pattern for integration tests to target within test directories 
in <root>/src/Tests/Elastic.Installer.Integration.Tests/Tests.

When not specified, defaults to *

VagrantProvider:
----------------

The provider that vagrant should use to bring up vagrant boxes
    - local: use Virtualbox on the local machine
    - azure: use Azure provider to provision a machine on Azure for each integration test scenario
    - quick-azure: use Azure provider to provision a single machine on Azure on which to run all integration tests sequentially

skiptests:
----------

Whether to skip unit tests.

switches:
---------

Integration tests against a local vagrant provider support several switches
    - -gui: launch vagrant with a GUI
    - -nodestroy: do not destroy the vagrant box after the test has run
    - -plugins:<comma separated plugins>: a list of plugin zips that exist within
                                          the build/in directory, that should be installed
                                          within integration tests instead of downloading. The plugin
                                          zip names must match the installer version.

"""

    [<Literal>]
    let private feedUrl = "https://www.elastic.co/downloads/past-releases/feed"
    [<Literal>]
    let private feedExample = "feed-example.xml"
    
    type DownloadFeed = XmlProvider< feedExample >

    type VersionRegex = Regex< @"^(?:\s*(?<Product>.*?)\s*)?((?<Source>\w*)\:)?(?<Version>(?<Major>\d+)\.(?<Minor>\d+)\.(?<Patch>\d+)(?:\-(?<Prerelease>[\w\-]+))?)", noMethodPrefix=true >

    let private parseSource = function
        | "r" -> Released
        | hash when isNotNullOrEmpty hash -> BuildCandidate hash
        | _ -> Compile

    let parseVersion version =
        let m = VersionRegex().Match version
        if m.Success |> not then failwithf "Could not parse version from %s" version
        let source = parseSource m.Source.Value

        let rawValue =
            match source with
            | Compile -> m.Version.Value
            | _ -> sprintf "%s:%s" m.Source.Value m.Version.Value

        { Product = m.Product.Value;
          FullVersion = m.Version.Value;
          Major = m.Major.Value |> int;
          Minor = m.Minor.Value |> int;
          Patch = m.Patch.Value |> int;
          Prerelease = m.Prerelease.Value; 
          Source = source;
          RawValue = rawValue; }

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
        [version]

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
            [version]
        | _ -> failwithf "Expecting one %s zip file in %s but found %i" product.Name InDir zips.Length


    let private args = getBuildParamOrDefault "cmdline" "buildinstallers" |> split ' '
    let private skipTests = args |> List.exists (fun x -> x = "skiptests")
    let private gui = args |> List.exists (fun x -> x = "-gui")
    let private noDestroy = args |> List.exists (fun x -> x = "-nodestroy")
    let private plugins = args |> List.exists (startsWith "-plugins:")
    let private filteredArgs = args |> List.filter (fun x -> match x with
                                                             | "skiptests"
                                                             | "-gui"
                                                             | "-nodestroy" -> false
                                                             | y when startsWith "-plugins:" y -> false
                                                             | _ -> true)

    let private (|IsTarget|_|) (candidate: string) =
        match candidate.ToLowerInvariant() with
        | "buildservices"
        | "buildinstallers"
        | "test"
        | "clean"
        | "downloadproducts"
        | "patchguids"
        | "unittest"
        | "prunefiles"
        | "release"
        | "integrate" -> Some candidate
        | _ -> None

    let target =
        match (filteredArgs |> List.tryHead) with
        | Some t -> 
            match (t.ToLowerInvariant()) with
            | IsTarget t -> t
            | "help" 
            | "?" -> "help"
            | _ -> "buildinstallers"
        | _ -> "buildinstallers"

    let arguments =
        match filteredArgs with
        | IsTarget head :: tail -> head :: tail
        | [] -> [target]
        | _ -> target :: filteredArgs

    let private (|IsVersionList|_|) candidate =
        let versionStrings = splitStr "," candidate
        let versions = new List<Version>()
        
        versionStrings
        |> List.iter(fun v ->
            let m = VersionRegex().Match v
            match m.Success with
            | true -> versions.Add({ Product = m.Product.Value;
                        FullVersion = m.Version.Value;
                        Major = m.Major.Value |> int;
                        Minor = m.Minor.Value |> int;
                        Patch = m.Patch.Value |> int;
                        Prerelease = m.Prerelease.Value; 
                        Source = parseSource m.Source.Value;
                        RawValue = v; })
            | _ -> ()
        )      
        match versions with
        | v when v.Count = versionStrings.Length -> Some (List.ofSeq v)
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
    
    let private (|IsVagrantProvider|_|) candidate =
        match candidate with 
        | "local"
        | "azure" 
        | "quick-azure" -> Some candidate
        | _ -> None

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
                           All |> List.map (ProductVersions.CreateFromProduct versionFromInDir)
                       | ["release"; IsProductList products ] ->
                           setBuildParam "release" "1"
                           certAndPasswordFromEnvVariables ()
                           products |> List.map (ProductVersions.CreateFromProduct versionFromInDir)
                       | ["release"; IsVersionList versions ] ->
                           setBuildParam "release" "1"
                           certAndPasswordFromEnvVariables ()
                           All |> List.map (ProductVersions.CreateFromProduct <| fun _ -> versions)
                       | ["release"; IsProductList products; IsVersionList versions ] ->
                           setBuildParam "release" "1"
                           certAndPasswordFromEnvVariables ()
                           products |> List.map (ProductVersions.CreateFromProduct <| fun _ -> versions)
                       | ["release"; IsProductList products; IsVersionList versions; certFile; passwordFile ] ->
                           setBuildParam "release" "1"
                           certAndPasswordFromFile certFile passwordFile
                           products |> List.map (ProductVersions.CreateFromProduct <| fun _ -> versions)
                       | ["release"; IsVersionList versions; certFile; passwordFile ] ->
                           setBuildParam "release" "1"
                           certAndPasswordFromFile certFile passwordFile
                           All |> List.map (ProductVersions.CreateFromProduct <| fun _ -> versions)
                       | ["release"; IsProductList products; certFile; passwordFile ] ->
                           setBuildParam "release" "1"
                           certAndPasswordFromFile certFile passwordFile
                           products |> List.map (ProductVersions.CreateFromProduct versionFromInDir)
                       | ["release"; certFile; passwordFile ] ->
                           setBuildParam "release" "1"
                           certAndPasswordFromFile certFile passwordFile
                           All |> List.map (ProductVersions.CreateFromProduct versionFromInDir)

                       | ["integrate"; IsProductList products; IsVersionList versions; IsVagrantProvider provider; testTargets] ->
                           setBuildParam "testtargets" testTargets
                           setBuildParam "vagrantprovider" provider
                           products |> List.map (ProductVersions.CreateFromProduct <| fun _ -> versions)
                       | ["integrate"; IsProductList products; IsVersionList versions; testTargets] ->
                           setBuildParam "testtargets" testTargets
                           products |> List.map (ProductVersions.CreateFromProduct <| fun _ -> versions)
                           
                       | ["integrate"; IsProductList products; IsVersionList versions; IsVagrantProvider provider] ->
                           setBuildParam "vagrantprovider" provider
                           products |> List.map (ProductVersions.CreateFromProduct <| fun _ -> versions)
                       | ["integrate"; IsProductList products; IsVersionList versions] ->
                           products |> List.map (ProductVersions.CreateFromProduct <| fun _ -> versions)
                           
                       | ["integrate"; IsVersionList versions; IsVagrantProvider provider; testTargets] ->
                           setBuildParam "testtargets" testTargets
                           setBuildParam "vagrantprovider" provider
                           All |> List.map (ProductVersions.CreateFromProduct <| fun _ -> versions)                       
                       | ["integrate"; IsVersionList versions; testTargets] ->
                           setBuildParam "testtargets" testTargets
                           All |> List.map (ProductVersions.CreateFromProduct <| fun _ -> versions)
                           
                       | ["integrate"; IsProductList products; IsVagrantProvider provider; testTargets] ->
                           setBuildParam "testtargets" testTargets
                           setBuildParam "vagrantprovider" provider
                           products |> List.map (ProductVersions.CreateFromProduct lastFeedVersion)                           
                       | ["integrate"; IsProductList products; testTargets] ->
                           setBuildParam "testtargets" testTargets
                           products |> List.map (ProductVersions.CreateFromProduct lastFeedVersion)                    
                       | ["integrate"; IsProductList products; IsVagrantProvider provider] ->
                           setBuildParam "vagrantprovider" provider
                           products |> List.map (ProductVersions.CreateFromProduct lastFeedVersion) 
                       | ["integrate"; IsProductList products] ->
                           products |> List.map (ProductVersions.CreateFromProduct lastFeedVersion)        
                       | ["integrate"; IsVersionList versions; IsVagrantProvider provider] ->
                           setBuildParam "vagrantprovider" provider
                           All |> List.map (ProductVersions.CreateFromProduct <| fun _ -> versions)                       
                       | ["integrate"; IsVersionList versions] ->
                           All |> List.map (ProductVersions.CreateFromProduct <| fun _ -> versions)
                       | ["integrate"; IsVagrantProvider provider; testTargets] ->
                           setBuildParam "testtargets" testTargets
                           setBuildParam "vagrantprovider" provider
                           All |> List.map (ProductVersions.CreateFromProduct lastFeedVersion)      
                       | ["integrate"; IsVagrantProvider provider] ->
                           setBuildParam "vagrantprovider" provider
                           All |> List.map (ProductVersions.CreateFromProduct lastFeedVersion)                
                       | ["integrate"; testTargets] ->
                           setBuildParam "testtargets" testTargets
                           All |> List.map (ProductVersions.CreateFromProduct lastFeedVersion)
                       | [IsProductList products; IsVersionList versions] ->
                           products |> List.map(ProductVersions.CreateFromProduct <| fun _ -> versions)
                       | [IsProductList products] ->
                           products |> List.map(ProductVersions.CreateFromProduct lastFeedVersion)
                       | [IsVersionList versions] ->
                           All |> List.map(ProductVersions.CreateFromProduct <| fun _ -> versions)
                       | [IsTarget target; IsVersionList versions] ->
                           All |> List.map (ProductVersions.CreateFromProduct <| fun _ -> versions)
                       | [IsTarget target; IsProductList products] ->
                           products |> List.map (ProductVersions.CreateFromProduct lastFeedVersion)
                       | [IsTarget target; IsProductList products; IsVersionList versions] ->
                           products |> List.map (ProductVersions.CreateFromProduct <| fun _ -> versions)
                       | [IsTarget target] ->
                           All |> List.map (ProductVersions.CreateFromProduct lastFeedVersion)
                       | [] ->
                           All |> List.map (ProductVersions.CreateFromProduct lastFeedVersion)
                       | _ ->
                           traceError usage
                           exit 2

        setBuildParam "target" target
        if skipTests then setBuildParam "skiptests" "1"
        if gui then setBuildParam "gui" "$true"
        if noDestroy then setBuildParam "no-destroy" "$false"
        if plugins then
            let pluginPaths = args 
                              |> List.find (startsWith "-plugins:") 
                              |> split ':'
                              |> List.last
            setBuildParam "plugins" pluginPaths
        products

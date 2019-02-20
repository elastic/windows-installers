#I "../../packages/build/FAKE.x64/tools"
#I @"../../packages/build/Fsharp.Data/lib/net40"
#I @"../../packages/build/FSharp.Text.RegexProvider/lib/net40"

#r @"FakeLib.dll"
#r "FSharp.Data.dll"
#r "Fsharp.Text.RegexProvider.dll"
#r "System.Xml.Linq.dll"
#load "Paths.fsx"
#load "Products.fsx"
#load "Artifacts.fsx"

open System
open System.IO
open System.Net
open Fake
open Paths
open Products
open Artifacts

ServicePointManager.SecurityProtocol <- SecurityProtocolType.Ssl3 |||
                                        SecurityProtocolType.Tls |||
                                        SecurityProtocolType.Tls11 |||
                                        SecurityProtocolType.Tls12

module Commandline =
    open Paths

    let usage = """
USAGE:

build.bat [Target] [RequestedArtifacts] [Target specific params] [skiptests]

Target:
-------

* listartifacts
  - lists all available staging and snapshot artifacts
      
* resolve [RequestedArtifacts]
  - output results of resolving requested artifacts
  
  Example: build.bat resolve es:6:zip:staging

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

* release [Product|RequestedArtifacts] [CertFile] [PasswordFile]
  - create a release versions of each MSI by building and then signing the service executable and installer for each.
  - when CertFile and PasswordFile are specified, these will be used for signing otherwise the values in ELASTIC_CERT_FILE
    and ELASTIC_CERT_PASSWORD environment variables will be used

  Examples:
  
  build.bat release es
  
  build.bat release es 5.5.3 C:/path_to_cert_file C:/path_to_password_file
  
* integrate [RequestedArtifacts] [VagrantProvider] [TestTargets] [switches] [skiptests]  -
  - run integration tests. Can filter tests by wildcard [TestTargets], 
    which match against the directory names of tests

  Example: build.bat integrate es 5.5.1,5.5.2 local * skiptests

* help or ?
  - show this usage summary

RequestedArtifacts:
-------------------

    Optional requested artifacts for build targets. Multiple requested artifacts can be specified, separated by commas.
    Components of a version string are seperated by colons and refer to:
        [Product]:[RequestedVersion]:[Distribution]:[Source]
    
    A requested artifact is eventually resolved to a resolved artifact. To test this process it is possible to call:
        
        build.bat resolve [RequestedArtifacts]
    
    This will output the resolved asset.
    
    The version string format is explained below:
    
    [Product]
    
        e / es / elasticsearch = Elasticsearch
        k / kibana             = Kibana
    
    [RequestedVersion]
    
        Can refer to a complete version (Major.Minor.Patch-Prerelease) or can use wildcards (*) to
        denote latest versions. If no prerelease part is specified, then stable versions are only considered.
        Examples:
        7 / 7.* / 7.*.* = Latest stable (not alpha1, beta1, beta2, rc1...) 7 version
        7-*             = Latest 7 version (including prereleases)
        7-beta1         = Latest 7 version with beta1 prerelease moniker
        7.5-rc1         = Latest 7.5 minor version with rc1 prerelease moniker
        6.5 / 6.5.*     = Latest stable (not alpha1, beta1, beta2, rc1...) patch release in the 6.5 minor version
        * / *.*.*       = Latest stable version (not alpha1, beta1, beta2, rc1...)
        *-* / *.*.*-*   = Latest version (including prereleases)
    
    [Distribution]
    
        official = Official releases for general public download
        staging  = Build candidates for official release
        snapshot = On-demand and nightly builds
    
    [Source]
    
        zip = Bundled ZIP version
        msi = Compiled MSI from bundled ZIP version (typically used for integration tests)
    
    Complete examples
    -----------------
    
    Examples of complete requested artifacts:
    
        es:7-alpha1:zip:staging = Latest Elasticsearch 7 alpha1 prerelease from staging
        es:6.4.1:zip:official   = Elasticsearch ZIP 6.4.1 from official
        es:6.4:msi:official     = Latest patch version of Elasticsearch 6.4.* MSI from official
        es:6.4:msi:official     = Latest patch version of Elasticsearch 6.4.* MSI from official
        es:6.x:msi:official     = Latest minor version of Elasticsearch 6.* MSI from official
        es:6:msi:official       = Latest version of Elasticsearch 6 MSI from official
        es:6:msi:snaphost       = Latest version of Elasticsearch 6 MSI from snapshot
        es:6.x                  = Latest version of Elasticsearch 6 ZIP from official
        es                      = Latest version (including prereleases) of Elasticsearch ZIP from official
        es:92839eab             = Elasticsearch from snapshot with build hash 92839eab

    When specified, for build targets other than release, the product version zip files will
    be downloaded and extracted to build/in directory if they don't already exist.

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
        -gui: launch vagrant with a GUI
        -nodestroy: do not destroy the vagrant box after the test has run
        -plugins:<comma separated plugins>: a list of plugin zips that exist within
                                          the build/in directory, that should be installed
                                          within integration tests instead of downloading. The plugin
                                          zip names must match the installer version.

"""

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
        | "resolve"
        | "listartifacts"
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
        match filteredArgs |> List.tryHead with
        | Some t -> 
            match toLower t with
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
        let requestedAssets =
            match arguments with
            | [ "release"; IsProduct p ] ->
               setBuildParam "release" "1"
               certAndPasswordFromEnvVariables ()
               [ RequestedArtifact.fromDir p InDir ]
               
            | [ "release"; IsRequestedArtifactList requestedArtifacts ] ->
               setBuildParam "release" "1"
               certAndPasswordFromEnvVariables ()
               requestedArtifacts
               
            | [ "release"; IsRequestedArtifactList requestedArtifacts; certFile; passwordFile ] ->
               setBuildParam "release" "1"
               certAndPasswordFromFile certFile passwordFile
               requestedArtifacts
            
            | [ "release"; IsProduct p; certFile; passwordFile ] ->
               setBuildParam "release" "1"
               certAndPasswordFromFile certFile passwordFile
               [ RequestedArtifact.fromDir p InDir ]
            
            | [ "integrate"; IsRequestedArtifactList requestedArtifacts; IsVagrantProvider provider; testTargets] ->
               setBuildParam "vagrantprovider" provider
               setBuildParam "testtargets" testTargets
               requestedArtifacts
               
            | [ "integrate"; IsRequestedArtifactList requestedArtifacts; IsVagrantProvider provider] ->
               setBuildParam "vagrantprovider" provider
               requestedArtifacts
               
            | [ "integrate"; IsRequestedArtifactList requestedArtifacts; testTargets] ->
               setBuildParam "testtargets" testTargets
               requestedArtifacts
                                     
            | [ "integrate"; IsRequestedArtifactList requestedArtifacts] -> requestedArtifacts
               
            | [ "integrate"; IsVagrantProvider provider; testTargets] ->
               setBuildParam "vagrantprovider" provider
               setBuildParam "testtargets" testTargets
               [ RequestedArtifact.LatestElasticsearch ]                      
               
            | [ "integrate"; IsRequestedArtifactList requestedArtifacts; testTargets] ->
               setBuildParam "testtargets" testTargets
               requestedArtifacts  
                                    
            | [ IsTarget target; IsRequestedArtifactList requestedArtifacts ] -> requestedArtifacts
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
        requestedAssets

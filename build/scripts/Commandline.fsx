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
    let usage = """
USAGE:

build.bat [Target] [RequestedArtifacts] [Target specific params] [skiptests]

-------
Target:
-------

The following build targets are supported:

* listartifacts
  -------------
  
  - lists all available staging and snapshot artifacts
      
* resolve [RequestedArtifacts]
  ----------------------------

  - output results of resolving requested artifacts
  
  Example: build.bat resolve es:6:zip:staging

* buildinstallers
  ---------------

  - default target if none provided. Builds installers for requested artifacts

* buildservices
  -------------

  - Builds services for products

* clean
  -----

  - cleans build output folders

* patchguids
  ----------

  - ensures a product GUID exists for the specified products and versions

* unittest
  --------

  - build and unit test

* downloadproducts
  ----------------

  - downloads the products if not already downloaded, and unzips them
    if not already unzipped

* release [Product|RequestedArtifacts] [CertFile] [PasswordFile]
  --------------------------------------------------------------

  - create a release MSI(s)
    
    If a product is passed _without_ a version, a single zip file will be searched for within the build/in directory
    and version information extracted from that.
    
    If a list of requested artifacts is passed, a release version of an MSI will be created for each zip artifact
    by building and then signing the service executable and installer for each
  
    when CertFile and PasswordFile are specified, these will be used for signing,
    otherwise the values in ELASTIC_CERT_FILE and ELASTIC_CERT_PASSWORD environment variables will be used

  Examples:
  
  Creates an Elasticsearch MSI from a single zip file found in build/in
  
        build.bat release es
        
  Creates an Elasticsearch MSI from the official Elasticsearch 5.5.3 zip release 
  
        build.bat release es:5.5.3 C:/path_to_cert_file C:/path_to_password_file
  
* integrate [RequestedArtifacts] [VagrantProvider] [TestTargets] [switches] [skiptests]
  -------------------------------------------------------------------------------------
  
  - run integration tests for requested artifacts. The last requested artifact of a product is the target version for integration tests.
    Can filter tests by wildcard [TestTargets] which match against the directory names of tests

  Examples:
  
  Runs integration tests on Azure using a single VM, including upgrade tests, for the latest staging release of Elasticsearch 6.x MSI.
  The MSI to upgrade from is the latest official Elasticsearch 5.x MSI release.
  
        build.bat integrate es:5:msi,es:6:msi:staging quick-azure * skiptests
        
  Runs integration tests locally for an MSI compiled from the latest snapshot zip release of Elasticsearch.

        build.bat integrate es:x:snapshot local * skiptests

* help
  ----
  
  - show this usage summary

-------------------
RequestedArtifacts:
-------------------

    Optional requested artifacts for build targets. Multiple requested artifacts can be specified, separated by commas.
    Components of a version string are seperated by colons and refer to:
        [Product]:[RequestedVersion]:[Distribution]:[Source]
    
    A requested artifact is eventually resolved to a resolved artifact. To test this process it is possible to call:
        
        build.bat resolve [RequestedArtifacts]
    
    Which will output the resolved artifact.
    
    The version string format is explained below:
    
    [Product]
    
        e / es / elasticsearch = Elasticsearch
        k / kibana             = Kibana
    
    [RequestedVersion]
    
        Can refer to a complete version (Major.Minor.Patch-Prerelease) or can use wildcard (x) to
        denote latest versions.
        
        Examples:
        x                       = Latest version
        7 / 7.x                 = Latest 7.x version
        7.0.0                   = Latest 7.0.0 version
        7.0.0-beta1             = Latest 7.0.0 version beta1 prerelease
        7.0-SNAPSHOT            = Latest 7.0.x snapshot
        7.0.0-SNAPSHOT-3e96d60c = Specific 7.0.0 snapshot with build id 3e96d60c
        3e96d60c                = Snapshot or staging build with id 3e96d60c
        
    [Source]
    
        zip = Bundled ZIP version from which to compile an MSI
        msi = Already Compiled MSI, typically used for integration tests

    
    [Distribution]
    
        official = Official releases for general public download
        staging  = Staging Build candidates for official release
        snapshot = Snapshot On-demand and nightly builds
     
    Complete examples
    -----------------
    
    Examples of complete requested artifacts:
    
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

------------
TestTargets:
------------

    Wildcard pattern for integration tests to target within test directories 
    in <root>/src/Tests/Elastic.Installer.Integration.Tests/Tests.

    When not specified, defaults to *

----------------
VagrantProvider:
----------------

    The provider that vagrant should use to bring up vagrant boxes
        - local: use Virtualbox on the local machine
        - azure: use Azure provider to provision a machine on Azure for each integration test scenario
        - quick-azure: use Azure provider to provision a single machine on Azure on which to run all integration tests sequentially

----------
skiptests:
----------

    Whether to skip unit tests.

---------
switches:
---------

    Integration tests against a local vagrant provider support several switches
    
        -gui: launch vagrant with a GUI
        
        -nodestroy: do not destroy the vagrant box after the test has run. Useful when running
                    integration tests locally, and wish to inspect the state of the vagrant box after
                    the test has finished, successfully or not.
        
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

    let private (|IsTarget|_|) candidate =
        match candidate |> toLower with
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
        | "integrate"
        | "help" -> Some candidate
        | _ -> None

    let target =
        match filteredArgs |> List.tryHead with
        | Some t -> 
            match toLower t with
            | IsTarget t -> t
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
        let requestedArtifacts =
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
            | [ "listartifacts" ]
            | [ "help" ] -> []
            | _ ->
               sprintf "Could not parse target and arguments from %A" args |> traceError 
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
        
        requestedArtifacts

#I "../../packages/build/FAKE/tools"
#I "../../packages/build/System.Management.Automation/lib/net45"
#r "FakeLib.dll"
#r "System.Management.Automation.dll"
#load "Download.fsx"
#load "BuildConfig.fsx"

open System
open System.IO
open System.Management.Automation
open System.Text.RegularExpressions
open Fake
open Fake.FileHelper
open Scripts
open Scripts.Downloader
open Fake.Testing.XUnit2

let buildDir = "./build/"
let toolsDir = buildDir @@ "tools/"
let inDir = buildDir @@ "in/"
let outDir = buildDir @@ "out/"
let resultsDir = buildDir @@ "results/"

let version =
    let extractVersion (fileInfo:FileInfo) = 
        Regex.Replace(fileInfo.Name, "^elasticsearch\-(.*)\.zip$", "$1")
    let explicitVersion = getBuildParam "version"
    if isNullOrEmpty explicitVersion 
    then 
        match getBuildParam "release" with
        | "1" -> 
            let zips = inDir 
                       |> directoryInfo 
                       |> filesInDirMatching "elasticsearch*.zip"
            match zips.Length with
            | 0 -> failwithf "No elasticsearch zip file found in %s" inDir
            | 1 -> extractVersion zips.[0]
            | _ -> failwithf "Expecting one elasticsearch zip file in %s but found %i" inDir zips.Length
        | _ -> Downloader.lastVersion()
    else explicitVersion

tracefn "Starting build for version %s" version

let msiDir = "./src/Elastic.Installer.Msi/"
let msiBuildDir = msiDir @@ "bin/Release/"

let esBinDir = inDir @@ "elasticsearch-" + version @@ "/bin/"
let esServiceDir = "./src/Elasticsearch/Elastic.Installer.Elasticsearch.Process/"
let esServiceBuildDir = esServiceDir @@ "bin/AnyCPU/Release/"

let integrationTestsDir = FullName "./src/Tests/Elastic.Installer.Integration.Tests"
let unitTestsDir = "src/Tests/Elastic.Installer.Domain.Tests"

Target "Clean" (fun _ ->
    CleanDirs [msiBuildDir; esServiceBuildDir; outDir; resultsDir]
)

Target "DownloadProducts" (fun () ->
  Downloader.downloadProduct Product.Elasticsearch version
  //Downloader.downloadProduct Product.Kibana version
)

Target "UnzipProducts" (fun () ->
  Downloader.unzipProduct Product.Elasticsearch version
  //Downloader.unzipProduct Product.Kibana version
)

Target "PatchGuids" (fun () ->
    tracefn "Making sure a guid exists for v%s" version
    BuildConfig.versionGuid version |> ignore
)

Target "UnitTest" (fun () ->
    let unitTestBuildDir = unitTestsDir @@ "bin" @@ "Release"

    !! (unitTestsDir @@ "*.csproj")
    |> MSBuildRelease unitTestBuildDir "Build"
    |> Log "MsiBuild-Output: "

    !! (unitTestBuildDir @@ "*Tests.dll")
        |> xUnit2 (fun p -> { p with HtmlOutputPath = Some (resultsDir @@ "xunit.html") })
)

Target "PruneFiles" (fun () ->
  let keep = ["elasticsearch-plugin.bat"] |> Seq.map (fun n -> esBinDir @@ n)
  for file in System.IO.Directory.EnumerateFiles(esBinDir) do
        if keep |> Seq.exists (fun n -> n <> file) then System.IO.File.Delete(file))

let signFile file (product : Product) =
    let signToolExe = toolsDir @@ "signtool/signtool.exe"

    let certificate =
        let path = getBuildParam "certificate"
        if (isNullOrEmpty path) 
        then Environment.GetEnvironmentVariable("ELASTIC_CERT_FILE", EnvironmentVariableTarget.Machine)
        elif fileExists path then path
        else failwithf "Certificate not found at %s" path
    
    let password = 
        let path = getBuildParam "password"
        if (isNullOrEmpty path) 
        then Environment.GetEnvironmentVariable("ELASTIC_CERT_PASSWORD", EnvironmentVariableTarget.Machine)
        elif fileExists path then File.ReadAllText path
        else failwithf "Password not found at %s" path    

    let timestampServer = "http://timestamp.comodoca.com"
    let timeout = TimeSpan.FromMinutes 1.
    let description = System.Globalization.CultureInfo.GetCultureInfo(System.Threading.Thread.CurrentThread.CurrentCulture.Name).TextInfo.ToTitleCase product.Name

    if certificate <> null && password <> null
    then ExecProcess(fun info ->
            info.FileName <- signToolExe
            info.Arguments <- ["sign"; "/f"; certificate; "/p"; password; "/t"; timestampServer; "/d"; description; "/v"; file] |> String.concat " "
         ) <| timeout |> ignore
    else failwithf "Failed to sign %s: Certificate not found." file

// TODO need to make this more generic to handle other services eventually
let buildService (product : Product) sign =
    !! (esServiceDir @@ "*.csproj")
    |> MSBuildRelease esServiceBuildDir "Build"
    |> Log "ServiceBuild-Output: "

    let elasticsearchService = esBinDir @@ "elasticsearch.exe"
    CopyFile elasticsearchService (esServiceBuildDir @@ "Elastic.Installer.Elasticsearch.Process.exe")
    if sign then signFile elasticsearchService product |> ignore

let buildMsi (product : Product) sign =
    !! (msiDir @@ "*.csproj")
    |> MSBuildRelease msiBuildDir "Build"
    |> Log "MsiBuild-Output: "

    let buildFailure errors =
          raise (BuildException("Building" + product.Name + " MSI failed.", errors |> List.ofSeq))

    let timeout = TimeSpan.FromMinutes 20.
    let result = ExecProcess (fun info ->
                    info.FileName <- sprintf "%sElastic.Installer.Msi" msiBuildDir
                    info.WorkingDirectory <- msiDir
                    info.Arguments <- [product.Name; version; Path.GetFullPath(inDir)] |> String.concat " "
                 ) <| timeout

    if result <> 0 then raise (Exception())
    let finalMsi = (outDir @@ (sprintf "%s-%s.msi" product.Name version))
    CopyFile finalMsi (msiDir @@ (sprintf "%s.msi" product.Name))

    if sign then signFile finalMsi product |> ignore

Target "BuildServices" (fun () ->
    buildService Product.Elasticsearch false
    //buildService Product.Kibana false
)

Target "BuildInstallers" (fun () ->
    buildMsi Product.Elasticsearch false
    //buildMsi Product.Kibana false
)

Target "Sign" (fun () ->
    buildService Product.Elasticsearch true
    //buildService Product.Kibana true
    buildMsi Product.Elasticsearch true
    //buildMsi Product.Kibana false
)

Target "Release" (fun () ->
    trace "Building in release mode.  All files will be signed."
)

Target "Integrate" (fun () ->
  let integrationTestsTargets = getBuildParamOrDefault "testtargets" "*"
  let script = sprintf "cd '%s'; %s -Tests %s -Version %s" integrationTestsDir ".\Bootstrapper.ps1" integrationTestsTargets version
  trace (sprintf "Running Powershell script: \"%s\"" script)
  use p = PowerShell.Create()
  let output = new PSDataCollection<PSObject>()
  output.DataAdded.Add(fun data -> trace (sprintf "%O" output.[data.Index]))
  p.Streams.Verbose.DataAdded.Add(fun data -> trace (sprintf "%O" p.Streams.Verbose.[data.Index]))
  p.Streams.Debug.DataAdded.Add(fun data -> trace (sprintf "%O" p.Streams.Debug.[data.Index]))
  p.Streams.Progress.DataAdded.Add(fun data -> trace (sprintf "%O" p.Streams.Progress.[data.Index]))
  p.Streams.Warning.DataAdded.Add(fun data -> traceError (sprintf "%O" p.Streams.Warning.[data.Index]))
  p.Streams.Error.DataAdded.Add(fun data -> traceError (sprintf "%O" p.Streams.Error.[data.Index]))
  let async =
        p.AddScript(script).BeginInvoke(null, output)
              |> Async.AwaitIAsyncResult
              |> Async.Ignore
  Async.RunSynchronously async)

"Clean"
  =?> ("DownloadProducts", (not ((getBuildParam "release") = "1")))
  ==> "UnzipProducts"
  ==> "PatchGuids"
  ==> "PruneFiles"
  ==> "UnitTest"
  ==> "BuildServices"
  ==> "BuildInstallers"
  ==> "Integrate"

"UnitTest"
  ==> "Sign"
  ==> "Release"

RunTargetOrDefault "BuildInstallers"

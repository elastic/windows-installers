#I "../../packages/build/FAKE/tools"
#I "../../packages/build/System.Management.Automation/lib/net45"
#r "FakeLib.dll"
#r "System.Management.Automation.dll"
#load "Download.fsx"
#load "BuildConfig.fsx"

open System
open System.Diagnostics
open System.Text
open System.IO
open System.Management.Automation
open System.Text.RegularExpressions
open Microsoft.FSharp.Reflection
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

// TODO move these directory properties to Product
let esBinDir = inDir @@ "elasticsearch-" + version @@ "/bin/"
let esServiceDir = "./src/Elasticsearch/Elastic.Installer.Elasticsearch.Process/"
let esServiceBuildDir = esServiceDir @@ "bin/AnyCPU/Release/"

let kibanaBinDir = inDir @@ "kibana-" + version @@ "/bin/";
let kibanaServiceDir = "./src/Kibana/Elastic.Installer.Kibana.Process/"
let kibanaServiceBuildDir = kibanaServiceDir @@ "bin/AnyCPU/Release/"

let integrationTestsDir = FullName "./src/Tests/Elastic.Installer.Integration.Tests"
let unitTestsDir = "src/Tests/Elastic.Installer.Domain.Tests"

Target "Clean" (fun _ ->
    CleanDirs [msiBuildDir; esServiceBuildDir; outDir; resultsDir]
    [| Product.Elasticsearch; Product.Kibana |] 
    |> Array.iter(fun p -> CleanDirs [outDir @@ p.Name])
)

Target "DownloadProducts" (fun () ->
  if (not (Directory.Exists (Product.Elasticsearch.ExtractedDirectory version)))
  then 
    Downloader.downloadProduct Product.Elasticsearch version
    Downloader.unzipProduct Product.Elasticsearch version

  if (not (Directory.Exists (Product.Kibana.ExtractedDirectory version)))
  then 
    Downloader.downloadProduct Product.Kibana version
    Downloader.unzipProduct Product.Kibana version
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

let prune files directory =
  let keep = files |> Seq.map (fun n -> directory @@ n)
  for file in System.IO.Directory.EnumerateFiles(directory) do
        if keep |> Seq.exists (fun n -> n <> file) then System.IO.File.Delete(file)
        
Target "PruneFiles" (fun () ->
    prune ["elasticsearch-plugin.bat"] esBinDir
    prune ["kibana-plugin.bat"] kibanaBinDir
)

let signFile file (product : Product) =
    let getBuildParamOrEnvVariable param variable (valueFromFile:string -> string)  =
        let path = getBuildParam param
        if (isNullOrWhiteSpace path) 
        then 
            let env = Environment.GetEnvironmentVariable(variable, EnvironmentVariableTarget.Machine)
            if (isNullOrWhiteSpace env) then failwithf "No %s environment variable set" variable
            env
        elif fileExists path then valueFromFile path
        else failwithf "%s not found at %s" param path
            
    let certificate = getBuildParamOrEnvVariable "certificate" "ELASTIC_CERT_FILE" <| (fun f -> f)
    let password = getBuildParamOrEnvVariable "password" "ELASTIC_CERT_PASSWORD" <| File.ReadAllText
    let timestampServer = "http://timestamp.comodoca.com"
    let timeout = TimeSpan.FromMinutes 1.
    let description = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase product.Name

    let sign () =
        let signToolExe = toolsDir @@ "signtool/signtool.exe"
        let args = ["sign"; "/f"; certificate; "/p"; password; "/t"; timestampServer; "/d"; description; "/v"; file] |> String.concat " "
        let redactedArgs = args.Replace(password, "<redacted>")

        use proc = new Process()
        proc.StartInfo.UseShellExecute <- false
        proc.StartInfo.FileName <- signToolExe
        proc.StartInfo.Arguments <- args
        platformInfoAction proc.StartInfo             
        proc.StartInfo.RedirectStandardOutput <- true
        proc.StartInfo.RedirectStandardError <- true
        if isMono then
            proc.StartInfo.StandardOutputEncoding <- Encoding.UTF8
            proc.StartInfo.StandardErrorEncoding  <- Encoding.UTF8
        proc.ErrorDataReceived.Add(fun d ->
            if d.Data <> null then traceError d.Data)
        proc.OutputDataReceived.Add(fun d ->
            if d.Data <> null then trace d.Data)

        try
            tracefn "%s %s" proc.StartInfo.FileName redactedArgs
            start proc
        with exn -> failwithf "Start of process %s failed. %s" proc.StartInfo.FileName exn.Message
        proc.BeginErrorReadLine()
        proc.BeginOutputReadLine()
        if not <| proc.WaitForExit(int timeout.TotalMilliseconds) then
            try
                proc.Kill()
            with exn ->
                traceError
                <| sprintf "Could not kill process %s  %s after timeout." proc.StartInfo.FileName redactedArgs
            failwithf "Process %s %s timed out." proc.StartInfo.FileName redactedArgs
        proc.WaitForExit()
        proc.ExitCode

    let exitCode = sign()
    if exitCode <> 0 then failwithf "Signing %s returned error exit code: %i" description exitCode

let buildService (product : Product) sign serviceDir serviceBuildDir serviceBinDir =
    !! (serviceDir @@ "*.csproj")
    |> MSBuildRelease serviceBuildDir "Build"
    |> Log "ServiceBuild-Output: "
    let serviceAssembly = serviceBuildDir @@ (sprintf "Elastic.Installer.%s.Process.exe" product.Name)
    let service = serviceBinDir @@ (sprintf "%s.exe" product.Name)
    CopyFile service serviceAssembly
    if sign then signFile service product |> ignore

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
    let finalMsi = (outDir @@ product.Name @@ (sprintf "%s-%s.msi" product.Name version))
    CopyFile finalMsi (msiDir @@ (sprintf "%s.msi" product.Name))

    if sign then signFile finalMsi product |> ignore

Target "BuildServices" (fun () ->
    buildService Product.Elasticsearch false esServiceDir esServiceBuildDir esBinDir
    buildService Product.Kibana false kibanaServiceDir kibanaServiceBuildDir kibanaBinDir
)

Target "BuildInstallers" (fun () ->
    buildMsi Product.Elasticsearch false
    buildMsi Product.Kibana false
)

Target "Sign" (fun () ->
    buildService Product.Elasticsearch true esServiceDir esServiceBuildDir esBinDir
    buildService Product.Kibana true kibanaServiceDir kibanaServiceBuildDir kibanaBinDir
    buildMsi Product.Elasticsearch true
    buildMsi Product.Kibana true
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
  ==> "PatchGuids"
  ==> "PruneFiles"
//  ==> "UnitTest"
  ==> "BuildServices"
  ==> "BuildInstallers"
  ==> "Integrate"

"UnitTest"
  ==> "Sign"
  ==> "Release"

RunTargetOrDefault "BuildInstallers"

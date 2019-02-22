#I "../../packages/build/FAKE.x64/tools"
#I @"../../packages/build/Fsharp.Data/lib/net40"
#I "../../packages/build/System.Management.Automation/lib/net45"

#r "FakeLib.dll"
#r "FSharp.Data.dll"
#r "System.Xml.Linq.dll"
#r "System.Management.Automation.dll"

#load "Paths.fsx"
#load "Products.fsx"
#load "Artifacts.fsx"

open System
open System.IO
open System.Management.Automation
open System.Text
open Fake
open FSharp.Data
open Paths
open Artifacts

type private NUnitXml = XmlProvider< "nunit-example.xml" >

type NUnitResult =
    { Name: string
      Total: int
      Passed: int
      Failed: int
      Skipped: int
      Pending: int
      Inconclusive: int }

/// Runs integration tests using Vagrant, PowerShell and Pester    
let runIntegrationTests (artifacts: ResolvedArtifact list) =
    // TODO: Get these from Commandline let bindings?
    let integrationTestsTargets = getBuildParamOrDefault "testtargets" "*"
    let vagrantProvider = getBuildParamOrDefault "vagrantprovider" "local"
    let gui = getBuildParamOrDefault "gui" "$false"
    let noDestroy = getBuildParamOrDefault "no-destroy" "$true"
    let plugins = getBuildParamOrDefault "plugins" ""
    
    let requestedArtifactStrings = artifacts |> List.map (fun a -> a.Identifier)
    
    // last version in the artifact list is the target version for tests   
    let version = requestedArtifactStrings |> List.last    

    // copy any plugins specified to build/out
    if isNotNullOrEmpty plugins then
        let pluginNames = plugins.Split([|',';';'|], StringSplitOptions.RemoveEmptyEntries)
        artifacts
        |> List.collect(fun s ->
            pluginNames 
            |> Array.map(fun p -> InDir </> (sprintf "%s-%s.zip" p s.Version.FullVersion))
            |> Array.toList
        )
        |> List.iter(fun p ->
            match fileExists p with
            | true -> CopyFile OutDir p
            | false -> traceFAKE "%s does not exist. Will attempt to install from public url" p 
        )

    // construct PowerShell array notation of previous versions
    let previousVersions = 
        match requestedArtifactStrings.Length with
        | 1 -> "@()"
        | _ -> requestedArtifactStrings.[0..requestedArtifactStrings.Length - 2]
               |> List.map(fun v -> sprintf "'%s'" v)
               |> String.concat ","
               |> sprintf "@(%s)"
        
    let script = sprintf @"cd '%s'; .\Bootstrapper.ps1 -Tests '%s' -Version '%s' -PreviousVersions %s -VagrantProvider '%s' -Gui:%s -VagrantDestroy:%s" 
                    IntegrationTestsDir 
                    integrationTestsTargets 
                    version 
                    previousVersions 
                    vagrantProvider
                    gui
                    noDestroy
        
    sprintf "Running Powershell script: '%s'" script |> trace
    
    use p = PowerShell.Create()
    
    // harvest output from the PowerShell script execution
    use output = new PSDataCollection<PSObject>()
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
       
    // run PowerShell script 
    Async.RunSynchronously async

    if (p.InvocationStateInfo.State = PSInvocationState.Failed) then  
        failwithf "PowerShell completed abnormally due to an error: %A" p.InvocationStateInfo.Reason      
    
/// Prints the integration test results to standard output
let printTestResults () =
    let results =
        Directory.EnumerateFiles(OutDir, "*.xml")
        |> Seq.map (fun f ->
            let xml = File.OpenRead f |> NUnitXml.Load     
            { Name = (Path.GetFileName f).Substring(8)
              Total = xml.Total
              Passed = xml.Total - (xml.Failures + xml.Ignored + xml.Inconclusive + xml.NotRun)
              Failed = xml.Failures
              Skipped = xml.Ignored
              Inconclusive = xml.Inconclusive
              Pending = xml.NotRun })
        |> Seq.toList
        
    let total =
        results
        |> Seq.reduce (fun f s ->
           { Name = ""
             Total = f.Total + s.Total
             Passed = f.Passed + s.Passed
             Failed = f.Failed + s.Failed
             Skipped = f.Skipped + s.Skipped
             Inconclusive = f.Inconclusive + s.Inconclusive
             Pending = f.Pending + s.Pending })
        
    let failures =
        results        
        |> Seq.fold(fun (builder:StringBuilder) r ->
            if r.Failed > 0 then sprintf "%i %s\n" r.Failed r.Name |> builder.Append
            else builder) (new StringBuilder())
        |> fun builder ->
            if builder.Length > 0 then sprintf "Failures in files\n%s" (builder.ToString())
            else builder.ToString()
 
    tracefn """"
---------------------------------------------------------------------
Integration Test Results
---------------------------------------------------------------------

Total:           %i
Passed:          %i
Failed:          %i
Skipped:         %i
Inconclusive:    %i
Pending:         %i

%s
---------------------------------------------------------------------
    """ total.Total total.Passed total.Failed total.Skipped total.Inconclusive total.Pending failures

    

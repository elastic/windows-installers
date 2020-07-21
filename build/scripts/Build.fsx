#I "../../packages/build/FAKE.x64/tools"

#r "FakeLib.dll"

#load "Paths.fsx"
#load "Products.fsx"
#load "Versions.fsx"
#load "Artifacts.fsx"

open System
open System.Diagnostics
open System.Text
open Fake
open Fake.AssemblyInfoFile
open Fake.Git
open Artifacts
open Paths
open Products
open Versions

// use vswhere to set MSBuild location, if it exists. 
// Allows FAKE MsBuildHelper.MsBuildRelease to work with Visual Studio 2019
let private vsWhere = Environment.ExpandEnvironmentVariables("%ProgramFiles(x86)%") </> @"Microsoft Visual Studio\Installer\vswhere.exe"
if fileExists vsWhere then
    let result = ExecProcessAndReturnMessages (fun p -> 
                    p.FileName <- vsWhere
                    p.Arguments <- @"-latest -products * -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe"
                 ) (TimeSpan.FromSeconds 5.)

    if result.ExitCode = 0 then setEnvironVar "MSBuild" result.Messages.[0]

/// Signs a file using a certificate
let sign file productTitle =
    let release = getBuildParam "release" = "1"
    if release then
        let certificate = getBuildParam "certificate"
        let password = getBuildParam "password"
        let timestampServers =
            [ "http://timestamp.digicert.com" ; "http://timestamp.comodoca.com" ;
              "http://timestamp.globalsign.com/scripts/timestamp.dll" ; "http://tsa.starfieldtech.com" ;
              "http://zeitstempel.dfn.de"
            ]
        let timeout = TimeSpan.FromMinutes 1.

        let sign timestampServer =
            let signToolExe = ToolsDir @@ "signtool/signtool.exe"
            let args =
                [ "sign"; "/debug"; "/f"; certificate; "/p"; password
                  "/tr"; timestampServer; "/td"; "SHA256";
                  "/d"; productTitle; "/v"; file
                ] |> String.concat " "
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
            proc.ErrorDataReceived.Add(fun d -> if d.Data <> null then traceError d.Data)
            proc.OutputDataReceived.Add(fun d -> if d.Data <> null then trace d.Data)

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
            (timestampServer, proc.ExitCode)
        
        let validExitCode =
            timestampServers
            |> Seq.map sign
            |> Seq.takeWhile (fun (server, exitCode) -> exitCode <> 0)
            |> Seq.tryFind (fun (server, exitCode) -> exitCode = 0)
        
        match validExitCode with
        | Some (server, exitCode) ->  failwithf "Signing with a timestamp from %s failed with code: %i" server exitCode
        | None -> tracefn "signing succeeded" 

/// Patches the assembly information of the service executable for a resolved artifact
let patchServiceAssemblyInformation (resolvedArtifact: ResolvedArtifact) = 
    let version = resolvedArtifact.Version.FullVersion
    let commitHash = Information.getCurrentHash()
    let file = resolvedArtifact.ServiceDir @@ "Properties" @@ "AssemblyInfo.cs"
    CreateCSharpAssemblyInfo file
        [ Attribute.Title resolvedArtifact.Product.AssemblyTitle
          Attribute.Description resolvedArtifact.Product.AssemblyDescription
          Attribute.Guid resolvedArtifact.Product.AssemblyGuid
          Attribute.Product resolvedArtifact.Product.Title
          Attribute.Metadata("GitBuildHash", commitHash)
          Attribute.Company  "Elasticsearch BV"
          Attribute.Copyright "Apache License, version 2 (ALv2). Copyright Elasticsearch."
          Attribute.Trademark (sprintf "%s is a trademark of Elasticsearch BV, registered in the U.S. and in other countries." resolvedArtifact.Product.Title)
          Attribute.Version  version
          Attribute.FileVersion version
          Attribute.InformationalVersion version ] // Attribute.Version and Attribute.FileVersion normalize the version number, so retain the prelease suffix

/// Builds a service executable for a resolved artifact zip
let buildService (resolvedArtifact: ResolvedArtifact) =
    patchServiceAssemblyInformation resolvedArtifact
    
    !! (resolvedArtifact.ServiceDir @@ "*.csproj")
    |> MSBuildRelease resolvedArtifact.ServiceBinDir "Build"
    |> ignore
    
    let serviceAssembly = resolvedArtifact.ServiceBinDir @@ (sprintf "%s.exe" resolvedArtifact.Product.Name)
    let service = resolvedArtifact.BinDir @@ (sprintf "%s.exe" resolvedArtifact.Product.Name)
    CopyFile service serviceAssembly
    sign service resolvedArtifact.Product.Title
    
let mutable private builtMsi = false
    
/// Builds an MSI from the files located in a resolved artifact zip and copies to the output directory.
/// If the resolved artifact is an MSI, simply copies to the output directory
let buildAndCopyMsi (resolvedArtifact: ResolvedArtifact) =

    // Compile the MSI project only once
    if builtMsi = false then       
        !! (MsiDir @@ "*.csproj")
        |> MSBuildRelease MsiBuildDir "Build"
        |> ignore
        
        builtMsi <- true
    
    if not <| directoryExists resolvedArtifact.OutMsiDir then CreateDir resolvedArtifact.OutMsiDir   
    
    match resolvedArtifact.Distribution with
    | Zip ->
       let exitCode = ExecProcess (fun info ->
                        info.FileName <- sprintf "%sElastic.Installer.Msi" MsiBuildDir
                        info.WorkingDirectory <- MsiDir
                        info.Arguments <- [ resolvedArtifact.Product.Name;
                                            resolvedArtifact.Version.FullVersion;
                                            resolvedArtifact.ExtractedDirectory ]
                                          |> String.concat " "
                       ) <| TimeSpan.FromMinutes 20.
    
       if exitCode <> 0 then failwithf "Error building MSI for %s %s" resolvedArtifact.Product.Name (resolvedArtifact.Version.ToString())            
       CopyFile resolvedArtifact.OutMsiPath (MsiDir @@ (sprintf "%s.msi" resolvedArtifact.Product.Name))
       sign resolvedArtifact.OutMsiPath resolvedArtifact.Product.Title
    | _ ->
       // just copy the distribution
       if not <| fileExists resolvedArtifact.DownloadPath then failwithf "No file found at %s" resolvedArtifact.DownloadPath
       tracef "Copying MSI from %s to %s" resolvedArtifact.DownloadPath resolvedArtifact.OutMsiPath     
       CopyFile resolvedArtifact.OutMsiPath (resolvedArtifact.DownloadPath)
        
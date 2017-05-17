#I "../../packages/build/FAKE/tools"
#I "../../packages/build/System.Management.Automation/lib/net45"

#r "FakeLib.dll"
#r "System.Management.Automation.dll"
#load "BuildConfig.fsx"
#load "Products.fsx"
#load "Versioning.fsx"

open System
open System.Diagnostics
open System.Text
open System.IO
open System.Management.Automation
open System.Text.RegularExpressions
open Microsoft.FSharp.Reflection
open Fake
open Fake.FileHelper
open Fake.Testing.XUnit2
open Products.Products
open Products.Paths
open Products
open Versioning

module Builder =

    let Sign file (product : Product) =
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

        let sign () =
            let signToolExe = ToolsDir @@ "signtool/signtool.exe"
            let args = ["sign"; "/f"; certificate; "/p"; password; "/t"; timestampServer; "/d"; product.Title; "/v"; file] |> String.concat " "
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
        if exitCode <> 0 then failwithf "Signing %s returned error exit code: %i" product.Title exitCode

    let BuildService (product : Product) sign =
        !! (product.ServiceDir @@ "*.csproj")
        |> MSBuildRelease product.ServiceBinDir "Build"
        |> Log "ServiceBuild-Output: "
        let serviceAssembly = product.ServiceBinDir @@ (sprintf "Elastic.Installer.%s.Process.exe" product.Title)
        let service = (product.BinDir Versioning.Version) @@ (sprintf "%s.exe" product.Name)
        CopyFile service serviceAssembly
        if sign then Sign service product |> ignore

    let BuildMsi (product : Product) sign =
        let version = Versioning.Version

        !! (MsiDir @@ "*.csproj")
        |> MSBuildRelease MsiBuildDir "Build"
        |> Log "MsiBuild-Output: "

        let timeout = TimeSpan.FromMinutes 20.
        let exitCode = ExecProcess (fun info ->
                        info.FileName <- sprintf "%sElastic.Installer.Msi" MsiBuildDir
                        info.WorkingDirectory <- MsiDir
                        info.Arguments <- [product.Name; version; Path.GetFullPath(InDir)] |> String.concat " "
                       ) <| timeout

        if exitCode <> 0 then failwithf "Error building MSI for %s" product.Name
        let finalMsi = OutDir @@ product.Name @@ (sprintf "%s-%s.msi" product.Name version)
        CopyFile finalMsi (MsiDir @@ (sprintf "%s.msi" product.Name))

        if sign then Sign finalMsi product
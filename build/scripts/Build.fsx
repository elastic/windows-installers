#I "../../packages/build/FAKE/tools"

#r "FakeLib.dll"
#load "Products.fsx"
#load "BuildConfig.fsx"

open System
open System.Diagnostics
open System.Text
open System.IO
open System.Text.RegularExpressions
open Microsoft.FSharp.Reflection
open Fake
open Fake.FileHelper
open Fake.Testing.XUnit2
open Products.Products
open Products.Paths
open Products

module Builder =

    let Sign file (product : ProductVersion) =
        let release = getBuildParam "release" = "1"
        if release then
            let certificate = getBuildParam "certificate"
            let password = getBuildParam "password"
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

    let BuildService (product : ProductVersion) =
        !! (product.ServiceDir @@ "*.csproj")
        |> MSBuildRelease product.ServiceBinDir "Build"
        |> Log "ServiceBuild-Output: "
        let serviceAssembly = product.ServiceBinDir @@ (sprintf "Elastic.Installer.%s.Process.exe" product.Title)
        let service = product.BinDir @@ (sprintf "%s.exe" product.Name)
        CopyFile service serviceAssembly
        Sign service product

    let BuildMsi (product : ProductVersion) =
        !! (MsiDir @@ "*.csproj")
        |> MSBuildRelease MsiBuildDir "Build"
        |> Log "MsiBuild-Output: "

        let exitCode = ExecProcess (fun info ->
                        info.FileName <- sprintf "%sElastic.Installer.Msi" MsiBuildDir
                        info.WorkingDirectory <- MsiDir
                        info.Arguments <- [product.Name; product.Version.FullVersion; Path.GetFullPath(InDir)] |> String.concat " "
                       ) <| TimeSpan.FromMinutes 20.

        if exitCode <> 0 then failwithf "Error building MSI for %s" product.Name
        let finalMsi = OutDir @@ product.Name @@ (sprintf "%s-%s.msi" product.Name product.Version.FullVersion)
        CopyFile finalMsi (MsiDir @@ (sprintf "%s.msi" product.Name))
        Sign finalMsi product
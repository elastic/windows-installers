#I "../../packages/build/FAKE.x64/tools"

#r "FakeLib.dll"

open Fake

module Paths =
    let BuildDir = "./build/"
    let ToolsDir = BuildDir @@ "tools/"
    let InDir = BuildDir @@ "in/"
    let OutDir = BuildDir @@ "out/"
    let ResultsDir = BuildDir @@ "results/"

    let SrcDir = "./src/"
    let ProcessHostsDir = SrcDir @@ "ProcessHosts/"
    let MsiDir = SrcDir @@ "Installer/Elastic.Installer.Msi/"
    let MsiBuildDir = MsiDir @@ "bin/Release/"

    let IntegrationTestsDir = FullName "./src/Tests/Elastic.Installer.Integration.Tests"
    let UnitTestsDir = "src/Tests/Elastic.Domain.Tests"

    let ArtifactDownloadsUrl = "https://artifacts.elastic.co/downloads"
    
    let ArtifactVersionsUrl = "https://artifacts-api.elastic.co/v1/versions"  
    let ArtifactVersionBuildsUrl version = sprintf "%s/%s/builds" ArtifactVersionsUrl version    
    let ArtifactVersionBuildUrl version build = sprintf "%s/%s/builds/%s" ArtifactVersionsUrl version build

    let StagingDownloadsUrl product hash fullVersion = 
        sprintf "https://staging.elastic.co/%s-%s/downloads/%s/%s-%s.msi" fullVersion hash product product fullVersion

    let SnapshotDownloadsUrl product versionNumber hash fullVersion =
        sprintf "https://snapshots.elastic.co/%s-%s/downloads/%s/%s-%s.msi" versionNumber hash product product fullVersion
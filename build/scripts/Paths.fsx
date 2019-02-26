#I "../../packages/build/FAKE.x64/tools"

#r "FakeLib.dll"

open Fake

let BuildDir = FullName "./build/"
let ToolsDir = BuildDir @@ "tools/"
let InDir = BuildDir @@ "in/"
let OutDir = BuildDir @@ "out/"
let ResultsDir = BuildDir @@ "results/"

let SrcDir = FullName "./src/"
let ProcessHostsDir = SrcDir @@ "ProcessHosts/"
let MsiDir = SrcDir @@ "Installer/Elastic.Installer.Msi/"
let MsiBuildDir = MsiDir @@ "bin/Release/"

let IntegrationTestsDir = FullName "./src/Tests/Elastic.Installer.Integration.Tests"
let UnitTestsDir = "src/Tests/Elastic.Domain.Tests"
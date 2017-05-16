<#
.Synopsis
    Bootstraps and runs integration tests for the Windows Installer
.Description
    Ensures all preconditions are met to run integration tests e.g. vagrant installed, cygpath.exe in PATH, etc.
    Then runs all integration test directories in .\Tests specified by $Tests parameter
.Example
    Execute tests in all directories

    .\Bootstrapper.ps1
.Example
    Execute only tests in directories that end in "NoPlugins"

    .\Bootstrapper.ps1 "*NoPlugins"
.Parameter Tests
    is a wildcard to describe the test directories in .\Tests which contain tests to run
#>
[CmdletBinding()]
Param(
    [Parameter(Position=0, Mandatory=$false, ValueFromPipelineByPropertyName=$true)]
    [Alias("T")]
    [string] $Tests="*",

    [Parameter(Mandatory=$true)]
    [string] $Version
)

$currentDir = Split-Path -parent $MyInvocation.MyCommand.Path
Set-Location $currentDir

# load utils
. $currentDir\Common\Utils.ps1

# Change this to have more or less verbose messages
$solutionDir = $(Get-Item $currentDir).Parent.Parent.Parent.FullName
$buildOutDir = Join-Path -Path $solutionDir -ChildPath "build\out"

###############
# Preconditions
###############

Add-Vagrant
Test-AtlasToken
Test-HyperV

$installer = Get-Installer -location $buildOutDir
if ($installer -eq $null) {
    log "No installer found in $buildOutDir. Build the installer by running build.bat in the solution root" -l Error
    Exit
}

###########
# Run Tests
###########

$testDirs = Get-ChildItem "Tests\$Tests" -Dir
$Count = $($testDirs | measure).Count

log "running $Count test scenarios"

# copy files needed for each test
foreach ($dir in $testDirs) {
    log "Running tests in $dir"
	Copy-Item "$currentDir\Common\Vagrantfile" -Destination $dir -Force
    Invoke-IntegrationTests -location $dir -version $Version
}

Set-Location $currentDir

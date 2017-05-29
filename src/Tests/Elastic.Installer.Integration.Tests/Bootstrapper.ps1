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
	[ValidatePattern("\d+\.\d+\.\d+((?:\-[\w\-]+))?")]
    [string] $Version,

	[Parameter(Mandatory=$false)]
	[ValidateSet("local", "azure")] 
	[string] $VagrantProvider="local"
)

$ErrorActionPreference = "Stop"

$currentDir = Split-Path -parent $MyInvocation.MyCommand.Path
Set-Location $currentDir

# load utils
. $currentDir\Common\Utils.ps1

$solutionDir = $(Get-Item $currentDir).Parent.Parent.Parent.FullName
$buildOutDir = Join-Path -Path $solutionDir -ChildPath "build\out"

###############
# Preconditions
###############

$installer = Get-Installer -location $buildOutDir -Version $Version
if ($installer -eq $null) {
    log "No installer found in $buildOutDir. Build the installer by running build.bat in the solution root" -l Error
    Exit 1
}

$testDirs = Get-ChildItem "Tests\$Tests" -Directory
$testCount = $($testDirs | measure).Count

if ($testCount -eq 0) {
	log "No tests found matching pattern $Tests" -l Error
	Exit 0
}

Add-Vagrant

if ($VagrantProvider -eq "local") {
	Test-AtlasToken
	Test-HyperV
} 
else {
	Add-VagrantAzureProvider
	Add-VagrantAzureBox
}

###########
# Run Tests
###########

log "running $testcount test scenario(s)"
foreach ($dir in $testDirs) {  
    log "running tests in $dir"
	Copy-Item "$currentDir\common\Vagrantfile" -Destination $dir -Force

    if ($VagrantProvider -eq "local") {
	    Invoke-IntegrationTestsOnLocal -Location $dir -Version $Version
    } 
    else {
	    Invoke-IntegrationTestsOnAzure -Location $dir -Version $Version
    }
}

Set-Location $currentDir
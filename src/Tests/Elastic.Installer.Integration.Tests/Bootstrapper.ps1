<#
.Synopsis
    Bootstraps and runs integration tests for the Windows Installer
.Description
    Ensures all preconditions are met to run integration tests e.g. vagrant installed, cygpath.exe in PATH, etc.
    Then runs all integration test directories in .\Tests specified by $Tests parameter
.Example
    Execute tests in all directories

    .\Bootstrapper.ps1 -Version 5.4.0
.Example
    Execute only tests in directories that end in "NoPlugins" on a local vagrant box

    .\Bootstrapper.ps1 "*NoPlugins" -Version 5.4.0 -VagrantProvider local
.Parameter Tests
    is a wildcard to describe the test directories in .\Tests which contain tests to run
.Parameter Version
	the version of the product under test
.Parameter PreviousVersions
	previous versions of the product to test upgrades against
.Parameter VagrantProvider
	the vagrant provider to use:
	- local = local vagrant box with virtualbox provider
	- azure = vagrant box created within Azure with azure provider. A box is created for each test scenario
	- quickazure = vagrant box created within Azure with azure provider. One box is created and all tests are run on it in sequence
.Parameter Gui
	whether to launch vagrant with a gui. Only valid for local vagrant provider
.Parameter VagrantDestroy
	wether to destroy the vagrant box on test finish. Only valid for local vagrant provider
#>
[CmdletBinding()]
Param(
    [Parameter(Position=0, Mandatory=$false, ValueFromPipelineByPropertyName=$true)]
    [Alias("T")]
    [string] $Tests="*",

    [Parameter(Mandatory=$true)]
	[ValidatePattern("^(?:(?<Product>\w*)\:)?(?<Version>(?<Major>\d+)\.(?<Minor>\d+)\.(?<Patch>\d+)(?:\-(?<Prerelease>[\w\-]+))?)(?:\:(?<Source>\w*))?(?:\:(?<Distribution>\w*))?(?:\:(?<BuildId>\w*))?$")]
    [string] $Version,    
    
    [Parameter(Mandatory=$false)]
	[ValidateScript({ $_ | ForEach-Object { $_ -match "^(?:(?<Product>\w*)\:)?(?<Version>(?<Major>\d+)\.(?<Minor>\d+)\.(?<Patch>\d+)(?:\-(?<Prerelease>[\w\-]+))?)(?:\:(?<Source>\w*))?(?:\:(?<Distribution>\w*))?(?:\:(?<BuildId>\w*))?$" } })]
    [string[]] $PreviousVersions=@(),

	[Parameter(Mandatory=$false)]
	[ValidateSet("local", "azure", "quick-azure")] 
	[string] $VagrantProvider="local",

	[switch] $Gui,

	[switch] $VagrantDestroy
)

$currentDir = Split-Path -Parent $MyInvocation.MyCommand.Path
cd $currentDir

. $currentDir\Common\Utils.ps1
. $currentDir\Common\Artifact.ps1

Set-Preferences

$solutionDir = $(Get-Item $currentDir).Parent.Parent.Parent.FullName
$buildOutDir = Join-Path -Path $solutionDir -ChildPath "build\out"

###############
# Preconditions
###############

$artifact = ConvertTo-Artifact $Version

$installer = Get-Installer -location $buildOutDir -Version $artifact
if ($installer -eq $null) {
    log "No $($artifact.FullVersion) installer found in $buildOutDir. Build the installer by running build.bat in the solution root" -l Error
    Exit 1
}

foreach($previousVersion in $PreviousVersions) {
	$artifact = ConvertTo-Artifact $previousVersion
	$installer = Get-Installer -location $buildOutDir -Version $artifact
	if ($installer -eq $null) {
		log "No $($artifact.FullVersion) installer found in $buildOutDir. Build the installer by running build.bat in the solution root" -l Error
		Exit 1
	}
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

# remove any files from previous run
Remove-Item "$currentDir\Tests\*" -Recurse -Force -Exclude *.ps1

###########
# Run Tests
###########

log "running $testcount test scenario(s)" -l Debug

Invoke-IntegrationTests -CurrentDir $currentDir -TestDirs $testDirs -VagrantProvider $VagrantProvider -Version $Version -PreviousVersions $PreviousVersions -Gui:$Gui -VagrantDestroy:$VagrantDestroy

cd $currentDir
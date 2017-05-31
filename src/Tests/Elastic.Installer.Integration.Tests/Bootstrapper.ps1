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
.Parameter VagrantProvider
	the vagrant provider to use:
	- local = local vagrant box with virtualbox provider
	- azure = vagrant box created within Azure with azure provider. A box is created for each test scenario
	- quickazure = vagrant box created within Azure with azure provider. One box is created and all tests are run on it in sequence
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
	[ValidateSet("local", "azure", "quick-azure")] 
	[string] $VagrantProvider="local"
)

$currentDir = Split-Path -parent $MyInvocation.MyCommand.Path
cd $currentDir

# load utils
. $currentDir\Common\Utils.ps1

Set-DebugMode

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

# run all tests on one vagrant box
if ($VagrantProvider -eq "quick-azure") {
	try {
		Copy-Item "$currentDir\common\Vagrantfile" -Destination $currentDir -Force
		$dnsName = Get-RandomName
		$testDirName = "Quick-Tests"
		$resourceGroupName = $testDirName + "-" + (Get-RandomName -Count $(59 - $testDirName.Length))
		$replacements = @{
			"AZURE_DNS_NAME" = $dnsName
			"AZURE_RESOURCE_GROUP_NAME" = $resourceGroupName
		}
		ReplaceInFile -File "Vagrantfile" -Replacements $replacements
	
		vagrant destroy azure -f
		vagrant up azure

		$session = [System.Management.Automation.Runspaces.PSSession](Get-WinRmSession -DnsName $dnsName)
		$syncFolders = @{
			"./Common/" = "/common"
      		"./../../../build/out/" = "/out"
		}

		Copy-SyncedFoldersToRemote -Session $session -SyncFolders $syncFolders
		foreach ($dir in $testDirs) {  
			log "running tests in $dir"
			Invoke-IntegrationTestsOnQuickAzure -Location $dir -Version $Version -Session $session
		}

		Remove-PSSession $session
	}
	catch {
		$ErrorMessage = $_.Exception.ToString()
		log $ErrorMessage -l Error
		Exit 1
	}
	finally {
		# don't wait for the destruction of the VM
		ReplaceInFile -File "Vagrantfile" -Replacements @{ "azure.wait_for_destroy = true" = "azure.wait_for_destroy = false" }   
		vagrant destroy azure -f
		Remove-Item "$currentDir\Vagrantfile" -Force -ErrorAction Ignore
    }
}
else {
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
}

cd $currentDir
<#
.Synopsis
    Bootstraps Pester on a Vagrant box and invokes the Tests in the $PWD.Drive.Root\vagrant directory
.Example
	.\PesterBootstrap.ps1 -Version "5.4.0" -TestDirName "Silent-Install"
.Example
	.\PesterBootstrap.ps1 -Version "5.4.0" -PreviousVersion "5.3.2" -TestDirName "Silent-Install"
.Parameter TestDirName
    The name of the test directory containing the tests
.Parameter Version
	The product version under test
.Parameter PreviousVersion
	The previous product version used to test upgrades and downgrades
#>
[CmdletBinding()]
Param(
    [Parameter(Mandatory=$true)]
    [string] $TestDirName,

    [Parameter(Mandatory=$true)]
	[ValidatePattern("\d+\.\d+\.\d+((?:\-[\w\-]+))?")]
    [string] $Version,

	[Parameter(Mandatory=$false)]
	[ValidatePattern("^$|\d+\.\d+\.\d+((?:\-[\w\-]+))?")]
    [string] $PreviousVersion
)

# Used in tests
$env:EsVersion = $Version

if ($PreviousVersion) {
	$env:PreviousEsVersion = $PreviousVersion
}

$currentDir = Split-Path -parent $MyInvocation.MyCommand.Path
cd $currentDir

$drive = $PWD.Drive.Root
$pester = "Pester"
$date = Get-Date -format "yyyy-MM-ddT-HHmmssfff"
$path = "$($drive)out\results-$TestDirName-$Version-$date.xml"

if(-not(Get-Module -Name $pester)) 
{ 
	# Load the Pester module into the current session. Install if not available
	Write-Output "import $pester"
	if(Get-Module -Name $pester -ListAvailable) { 
       	Import-Module -Name $pester 
    }  
	else { 
		PowerShellGet\Install-Module $pester -Force
		Import-Module $pester
	}
}

if ($PreviousVersion) {
	Invoke-Pester -Path "$($drive)vagrant\*" -OutputFile $path -OutputFormat "NUnitXml" -PassThru | Out-Null
}
else {
	Invoke-Pester -Path "$($drive)vagrant\*" -OutputFile $path -OutputFormat "NUnitXml" -ExcludeTag "PreviousVersion" -PassThru | Out-Null
}
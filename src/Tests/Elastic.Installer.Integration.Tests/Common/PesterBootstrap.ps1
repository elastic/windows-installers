[CmdletBinding()]
Param(
    [Parameter(Mandatory=$true)]
    [string] $TestDirName,

    [Parameter(Mandatory=$true)]
    [string] $Version
)

$currentDir = Split-Path -parent $MyInvocation.MyCommand.Path
Set-Location $currentDir

# Used in tests
$env:EsVersion = $Version

$pester = "Pester"
Write-Output "import $pester"
if(-not(Get-Module -Name $pester)) 
{ 
	if(Get-Module -Name $pester -ListAvailable) { 
       	Import-Module -Name $pester 
    }  
	else { 
		PowerShellGet\Install-Module $pester -Force
		Import-Module $pester
	}
}


$Date = Get-Date -format "yyyy-MM-ddT-HHmmssfff"
$Path = ".\..\out\results-$TestDirName-$Version-$Date.xml"

Invoke-Pester -Path '.\..\vagrant\*' -OutputFile $Path -OutputFormat "NUnitXml"

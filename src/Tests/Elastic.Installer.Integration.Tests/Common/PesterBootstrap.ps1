# TODO: Pass the name of the current test into here to include in the results file name

[CmdletBinding()]
Param(
    [Parameter(Mandatory=$true)]
    [string] $Version
)

$currentDir = Split-Path -parent $MyInvocation.MyCommand.Path
Set-Location $currentDir

$env:EsVersion = $Version

#Write-Host 'import pester'
Import-Module '.\..\pester\Pester.psm1'

$Date = Get-Date -format "yyyy-MM-ddT-HHmmssfff"
$Path = ".\..\out\results-$Date.xml"

#Write-Host 'run pester'
Invoke-Pester -Path '.\..\vagrant\*' -OutputFile $Path -OutputFormat "NUnitXml"
#Write-Host 'pester finished'

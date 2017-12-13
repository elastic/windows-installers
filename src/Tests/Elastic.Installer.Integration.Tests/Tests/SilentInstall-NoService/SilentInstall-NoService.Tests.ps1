$currentDir = Split-Path -parent $MyInvocation.MyCommand.Path
Set-Location $currentDir

# mapped sync folder for common scripts
. $currentDir\..\common\Utils.ps1
. $currentDir\..\common\CommonTests.ps1
. $currentDir\..\common\SemVer.ps1

Get-Version
Get-PreviousVersions

Describe "Silent Install do not install as service $(($Global:Version).Description)" {
    Invoke-SilentInstall @(,"INSTALLASSERVICE=false")

    Context-ElasticsearchServiceNotInstalled

    Copy-ElasticsearchLogToOut
}

Describe "Silent Uninstall do not install as service $(($Global:Version).Description)" {

    Invoke-SilentUninstall

	Context-EsConfigEnvironmentVariableNull

	Context-EsHomeEnvironmentVariableNull

	Context-MsiNotRegistered

	Context-ElasticsearchServiceNotInstalled

	$ProgramFiles = Get-ProgramFilesFolder
	$ChildPath = Get-ChildPath 
    $ExpectedHomeFolder = Join-Path -Path $ProgramFiles -ChildPath $ChildPath

	Context-EmptyInstallDirectory -Path $ExpectedHomeFolder
}
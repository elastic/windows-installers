$currentDir = Split-Path -parent $MyInvocation.MyCommand.Path
Set-Location $currentDir

# mapped sync folder for common scripts
. $currentDir\..\common\Utils.ps1
. $currentDir\..\common\CommonTests.ps1
. $currentDir\..\common\SemVer.ps1

Get-Version
Get-PreviousVersions

Describe "Silent Install as a manual service not started $(($Global:Version).Description)" {
    Invoke-SilentInstall @(,"INSTALLASSERVICE=true","STARTAFTERINSTALL=false","STARTWHENWINDOWSSTARTS=false")

    Context-ServiceRunningUnderAccount -Expected "LocalSystem"

	Context-ElasticsearchService -Expected @{
		Status="Stopped"
		StartType="Manual"
		StartIfNotRunning=$false
		CanStop=$false
		CanShutdown=$false
	}

	Get-ElasticsearchService | Start-Service

	Context-PingNode

    Copy-ElasticsearchLogToOut
}

Describe "Silent Uninstall as a manual service not started $(($Global:Version).Description)" {

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
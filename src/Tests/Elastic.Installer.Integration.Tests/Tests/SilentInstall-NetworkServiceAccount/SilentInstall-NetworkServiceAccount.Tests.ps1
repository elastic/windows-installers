$currentDir = Split-Path -parent $MyInvocation.MyCommand.Path
Set-Location $currentDir

# mapped sync folder for common scripts
. $currentDir\..\common\Utils.ps1
. $currentDir\..\common\CommonTests.ps1
. $currentDir\..\common\SemVer.ps1

Get-Version
Get-PreviousVersions

Describe "Silent Install as service with network service account $(($Global:Version).Description)" {
    Invoke-SilentInstall @("USENETWORKSERVICE=true","USELOCALSYSTEM=false")

    Context-ServiceRunningUnderAccount -Expected "NT AUTHORITY\NETWORKSERVICE"

	Context-ElasticsearchService

	Context-PingNode

    Copy-ElasticsearchLogToOut
}

Describe "Silent Uninstall as service with network service account $(($Global:Version).Description)" {

	$configDirectory = Get-ConfigEnvironmentVariableForVersion | Get-MachineEnvironmentVariable
	$dataDirectory = $configDirectory | Split-Path | Join-Path -ChildPath "data"
	$logsDirectory = $configDirectory | Split-Path | Join-Path -ChildPath "logs"

    Invoke-SilentUninstall

	Context-EsConfigEnvironmentVariableNull

	Context-EsHomeEnvironmentVariableNull

	Context-MsiNotRegistered

	Context-ElasticsearchServiceNotInstalled

	Context-EmptyInstallDirectory

	Context-DataDirectories -Path @($configDirectory, $dataDirectory, $logsDirectory) -DeleteAfter
}

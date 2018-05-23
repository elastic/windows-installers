$currentDir = Split-Path -parent $MyInvocation.MyCommand.Path
Set-Location $currentDir

# mapped sync folder for common scripts
. $currentDir\..\common\Utils.ps1
. $currentDir\..\common\CommonTests.ps1
. $currentDir\..\common\SemVer.ps1

Get-Version
Get-PreviousVersions

Describe -Name "Silent Install with setting up bootstrap password $(($Global:Version).Description)" -Tags @('XPack') {

	# don't try to install X-Pack for 6.3.0-SNAPSHOT+ releases
	$630SnapshotRelease = ConvertTo-SemanticVersion "6.3.0-SNAPSHOT"
	if ((Compare-SemanticVersion $Global:Version $630SnapshotRelease) -ge 0) {
		$plugins = ""
	} else {
		$plugins = "x-pack"
	}

	$exeArgs = @(
		"PLUGINS=$plugins", 
		"XPACKSECURITYENABLED=true", 
		"XPACKLICENSE=Trial", 
		"SKIPSETTINGPASSWORDS=true",
		"BOOTSTRAPPASSWORD=elastic")

    Invoke-SilentInstall -Exeargs $exeArgs

    Context-PingNode -XPackSecurityInstalled

    Context-PluginsInstalled -Expected @{ Plugins=($plugins.Split(@(','), [StringSplitOptions]::RemoveEmptyEntries)) }

    Context-ClusterNameAndNodeName -Expected @{ Credentials = "elastic:elastic" }

	Copy-ElasticsearchLogToOut
}

Describe -Name "Silent Uninstall with setting up bootstrap password $(($Global:Version).Description)" -Tags @('XPack') {

	$configDirectory = Get-ConfigEnvironmentVariableForVersion | Get-MachineEnvironmentVariable
	$dataDirectory = $configDirectory | Split-Path | Join-Path -ChildPath "data"
	$logsDirectory = $configDirectory | Split-Path | Join-Path -ChildPath "logs"

    Invoke-SilentUninstall

	Context-NodeNotRunning

	Context-EsConfigEnvironmentVariableNull

	Context-EsHomeEnvironmentVariableNull

	Context-MsiNotRegistered

	Context-ElasticsearchServiceNotInstalled

	Context-EmptyInstallDirectory

	Context-DataDirectories -Path @($configDirectory, $dataDirectory, $logsDirectory) -DeleteAfter
}
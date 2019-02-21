$currentDir = Split-Path -parent $MyInvocation.MyCommand.Path
Set-Location $currentDir

# mapped sync folder for common scripts
. $currentDir\..\common\Utils.ps1
. $currentDir\..\common\CommonTests.ps1
. $currentDir\..\common\SemVer.ps1

Get-Version
Get-PreviousVersions

$tags = @('XPack', 'Plugins')

# don't try to install X-Pack for 6.3.0-SNAPSHOT+ releases
$630SnapshotRelease = ConvertTo-SemanticVersion "6.3.0-SNAPSHOT"
if ((Compare-SemanticVersion $Global:Version $630SnapshotRelease) -ge 0) {
	$plugins = "ingest-geoip,ingest-attachment"
} else {
	$plugins = "x-pack,ingest-geoip,ingest-attachment"
}

Describe -Name "Silent Install with $plugins plugins $(($Global:Version).Description)" -Tags $tags {

    Invoke-SilentInstall -Exeargs @("PLUGINS=$plugins")

    Context-PingNode

    Context-PluginsInstalled -Expected @{ Plugins=($plugins.Split(@(','), [StringSplitOptions]::RemoveEmptyEntries)) }

    Context-ClusterNameAndNodeName

	Copy-ElasticsearchLogToOut
}

Describe -Name "Silent Uninstall with $plugin plugins $(($Global:Version).Description)" -Tags $tags {

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
$currentDir = Split-Path -parent $MyInvocation.MyCommand.Path
Set-Location $currentDir

# mapped sync folder for common scripts
. $currentDir\..\common\Utils.ps1
. $currentDir\..\common\CommonTests.ps1
. $currentDir\..\common\Artifact.ps1

Get-Version
Get-PreviousVersions

$InstallDir = "C:\temp dir\Elasticsearch\$($($Global:Version).FullVersion)\"
$DataDir = "C:\foo\data"
$ConfigDir = "C:\bar\config"
$LogsDir = "C:\baz\logs"

Describe "Silent Install with different install locations $(($Global:Version).Description)" {

    $InstallLocations = "INSTALLDIR=$InstallDir","DATADIRECTORY=$DataDir","CONFIGDIRECTORY=$ConfigDir","LOGSDIRECTORY=$LogsDir"

    Invoke-SilentInstall -ExeArgs $InstallLocations

    Context-ElasticsearchService

    Context-PingNode

    Context-EsHomeEnvironmentVariable -Expected $InstallDir

    Context-EsConfigEnvironmentVariable -Expected @{  
		Path = $ConfigDir
	}

    Context-PluginsInstalled

    Context-MsiRegistered

    Context-ServiceRunningUnderAccount -Expected "LocalSystem"

    Context-EmptyEventLog
  
    Context-ClusterNameAndNodeName

    Context-ElasticsearchConfiguration -Expected @{
		Data = $DataDir 
		Logs = $LogsDir 
	}

    Context-JvmOptions

	Copy-ElasticsearchLogToOut -Path "$LogsDir\elasticsearch.log"
}

Describe "Silent Uninstall with different install locations $(($Global:Version).Description)" {

    Invoke-SilentUninstall

	Context-NodeNotRunning

	Context-EsConfigEnvironmentVariableNull

	Context-EsHomeEnvironmentVariableNull

	Context-MsiNotRegistered

	Context-ElasticsearchServiceNotInstalled

	Context-EmptyInstallDirectory -Path $InstallDir

	Context-DataDirectories -Path @($ConfigDir, $DataDir, $LogsDir) -DeleteAfter
}
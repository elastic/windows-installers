$currentDir = Split-Path -parent $MyInvocation.MyCommand.Path
Set-Location $currentDir

# mapped sync folder for common scripts
. $currentDir\..\common\Utils.ps1
. $currentDir\..\common\CommonTests.ps1
. $currentDir\..\common\SemVer.ps1

Get-Version
Get-PreviousVersions

$InstallDir = "C:\temp dir\Elasticsearch\$($($Global:Version).FullVersion)\"
$DataDir = "C:\temp dir\Elasticsearch\$($($Global:Version).FullVersion)\data_data"
$ConfigDir = "C:\temp dir\Elasticsearch\$($($Global:Version).FullVersion)\config_config"
$LogsDir = "C:\temp dir\Elasticsearch\$($($Global:Version).FullVersion)\logs_logs"

Describe "Silent Install with same install locations $(($Global:Version).Description)" {

    $ExeArgs = @(
		"INSTALLDIR=$InstallDir"
		"DATADIRECTORY=$DataDir"
		"CONFIGDIRECTORY=$ConfigDir"
		"LOGSDIRECTORY=$LogsDir"
		"PLACEWRITABLELOCATIONSINSAMEPATH=true")

    Invoke-SilentInstall -ExeArgs $ExeArgs

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

Describe "Silent Uninstall with same install locations $(($Global:Version).Description)" {

    Invoke-SilentUninstall

	Context-NodeNotRunning

	Context-EsConfigEnvironmentVariableNull

	Context-EsHomeEnvironmentVariableNull

	Context-MsiNotRegistered

	Context-ElasticsearchServiceNotInstalled

	# install dir still exists because data, logs and config dirs exist within
	Context-DirectoryExists -Path $InstallDir

	Context-DataDirectories -Path @($ConfigDir, $DataDir, $LogsDir) -DeleteAfter
}
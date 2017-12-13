$currentDir = Split-Path -parent $MyInvocation.MyCommand.Path
Set-Location $currentDir

# mapped sync folder for common scripts
. $currentDir\..\common\Utils.ps1
. $currentDir\..\common\CommonTests.ps1
. $currentDir\..\common\SemVer.ps1

Get-Version
Get-PreviousVersions

$version = $Global:Version
$previousVersion = $Global:PreviousVersions[0]

$InstallDir = "D:\Elastic"
$DataDir = "D:\Data"
$ConfigDir = "D:\Config"
$LogsDir = "D:\Logs"
$tags = @('PreviousVersions') 

Describe -Name "Silent Install upgrade different volume install $($previousVersion.Description)" -Tags $tags {

	$v = $previousVersion.FullVersion
	$ExeArgs = "INSTALLDIR=$InstallDir\$v","DATADIRECTORY=$DataDir","CONFIGDIRECTORY=$ConfigDir","LOGSDIRECTORY=$LogsDir","PLUGINS=ingest-geoip"

    Invoke-SilentInstall -Exeargs $ExeArgs -Version $previousVersion

    Context-ElasticsearchService

    Context-PingNode

    Context-EsHomeEnvironmentVariable -Expected "$InstallDir\$v\"

    Context-EsConfigEnvironmentVariable -Expected @{ 
		Version = $previousVersion
		Path = $ConfigDir
	}

    Context-PluginsInstalled -Expected @{ Plugins=@("ingest-geoip") }

    Context-MsiRegistered -Expected @{
		Name = "Elasticsearch $v"
		Caption = "Elasticsearch $v"
		Version = "$($previousVersion.Major).$($previousVersion.Minor).$($previousVersion.Patch)"
	}

    Context-ServiceRunningUnderAccount -Expected "LocalSystem"

    Context-EmptyEventLog -Version $previousVersion

	Context-ClusterNameAndNodeName

    Context-ElasticsearchConfiguration -Expected @{
		Version = $previousVersion
		Data = $DataDir
		Logs = $LogsDir
	}

    Context-JvmOptions -Expected @{
		Version = $previousVersion
	}

	# Insert some data
	Context-InsertData
}

Describe -Name "Silent Install upgrade different volume from $($previousVersion.Description) to $($version.Description)" -Tags $tags {

	$v = $version.FullVersion
	$ExeArgs = "INSTALLDIR=$InstallDir\$v","DATADIRECTORY=$DataDir","CONFIGDIRECTORY=$ConfigDir","LOGSDIRECTORY=$LogsDir","PLUGINS=ingest-geoip"

    Invoke-SilentInstall -Exeargs $ExeArgs -Version $version -Upgrade

    Context-EsHomeEnvironmentVariable -Expected "$InstallDir\$v\"

    Context-EsConfigEnvironmentVariable -Expected @{ 
		Version = $version 
		Path = $ConfigDir
	}

	$expectedStatus = Get-ExpectedServiceStatus -Version $version -PreviousVersion $previousVersion

    Context-ElasticsearchService -Expected @{
		Status = $expectedStatus
	}

	Context-PingNode

    Context-PluginsInstalled -Expected @{ Plugins=@("ingest-geoip") }

    Context-MsiRegistered

    Context-ServiceRunningUnderAccount -Expected "LocalSystem"

    Context-EmptyEventLog -Version $previousVersion

	Context-ClusterNameAndNodeName

    Context-ElasticsearchConfiguration -Expected @{
		Version = $version
		Data = $DataDir
		Logs = $LogsDir
	}

    Context-JvmOptions -Expected @{
		Version = $version
	}

	# Check inserted data still exists
	Context-ReadData

	Copy-ElasticsearchLogToOut
}

Describe -Name "Silent Uninstall upgrade different volume uninstall $($version.Description)" -Tags $tags {

	$v = $version.FullVersion

    Invoke-SilentUninstall -Version $version

	Context-NodeNotRunning

	Context-EsConfigEnvironmentVariableNull

	Context-EsHomeEnvironmentVariableNull

	Context-MsiNotRegistered

	Context-ElasticsearchServiceNotInstalled

	Context-EmptyInstallDirectory -Path "$InstallDir\$($version.FullVersion)"
}
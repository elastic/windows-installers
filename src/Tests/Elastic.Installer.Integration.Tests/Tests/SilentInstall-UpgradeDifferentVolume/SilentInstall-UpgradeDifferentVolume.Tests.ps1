$currentDir = Split-Path -parent $MyInvocation.MyCommand.Path
Set-Location $currentDir

# mapped sync folder for common scripts
. $currentDir\..\common\Utils.ps1
. $currentDir\..\common\CommonTests.ps1
. $currentDir\..\common\SemVer.ps1

Get-Version
Get-PreviousVersions

$credentials = "elastic:changeme"
$version = $Global:Version
$previousVersion = $Global:PreviousVersions[0]

$InstallDir = "D:\Elastic"
$DataDir = "D:\Data"
$ConfigDir = "D:\Config"
$LogsDir = "D:\Logs"

Describe -Tag 'PreviousVersions' "Silent Install upgrade different volume - Install previous version $($previousVersion.Description)" {

	$v = $previousVersion.FullVersion
	$ExeArgs = "INSTALLDIR=$InstallDir\$v","DATADIRECTORY=$DataDir","CONFIGDIRECTORY=$ConfigDir","LOGSDIRECTORY=$LogsDir","PLUGINS=x-pack"

    Invoke-SilentInstall -Exeargs $ExeArgs -Version $previousVersion

    Context-ElasticsearchService

    Context-PingNode -XPackSecurityInstalled $true

    Context-EsHomeEnvironmentVariable -Expected "$InstallDir\$v"

    Context-EsConfigEnvironmentVariable -Expected @{ 
		Version = $previousVersion
		Path = $ConfigDir
	}

    Context-PluginsInstalled -Expected @{ Plugins=@("x-pack") }

    Context-MsiRegistered -Expected @{
		Name = "Elasticsearch $v"
		Caption = "Elasticsearch $v"
		Version = "$($previousVersion.Major).$($previousVersion.Minor).$($previousVersion.Patch)"
	}

    Context-ServiceRunningUnderAccount -Expected "LocalSystem"

    Context-EmptyEventLog

	Context-ClusterNameAndNodeName -Expected @{ Credentials = $credentials }

    Context-ElasticsearchConfiguration -Expected @{
		Version = $previousVersion
		Data = $DataDir
		Logs = $LogsDir
	}

    Context-JvmOptions -Expected @{
		Version = $previousVersion
	}

	# Insert some data
	Context-InsertData -Credentials $credentials
}

Describe -Tag 'PreviousVersions' "Silent Install upgrade different volume - Upgrade from $($previousVersion.Description) to $($version.Description)" {

	$v = $version.FullVersion
	$ExeArgs = "INSTALLDIR=$InstallDir\$v","DATADIRECTORY=$DataDir","CONFIGDIRECTORY=$ConfigDir","LOGSDIRECTORY=$LogsDir","PLUGINS=x-pack"

    Invoke-SilentInstall -Exeargs $ExeArgs -Version $version -Upgrade

    Context-EsHomeEnvironmentVariable -Expected "$InstallDir\$v"

    Context-EsConfigEnvironmentVariable -Expected @{ 
		Version = $version 
		Path = $ConfigDir
	}

	$expectedStatus = Get-ExpectedServiceStatus -Version $version -PreviousVersion $previousVersion

    Context-ElasticsearchService -Expected @{
		Status = $expectedStatus
	}

	Context-PingNode -XPackSecurityInstalled $true

    Context-PluginsInstalled -Expected @{ Plugins=@("x-pack") }

    Context-MsiRegistered

    Context-ServiceRunningUnderAccount -Expected "LocalSystem"

    Context-EmptyEventLog

	Context-ClusterNameAndNodeName -Expected @{ Credentials = $credentials }

    Context-ElasticsearchConfiguration -Expected @{
		Version = $version
		Data = $DataDir
		Logs = $LogsDir
	}

    Context-JvmOptions -Expected @{
		Version = $version
	}

	# Check inserted data still exists
	Context-ReadData -Credentials $credentials

	Copy-ElasticsearchLogToOut
}

Describe -Tag 'PreviousVersions' "Silent Uninstall upgrade different volume - Uninstall $($version.Description)" {

	$v = $version.FullVersion

    Invoke-SilentUninstall -Version $version

	Context-NodeNotRunning

	Context-EsConfigEnvironmentVariableNull

	Context-EsHomeEnvironmentVariableNull

	Context-MsiNotRegistered

	Context-ElasticsearchServiceNotInstalled

	Context-EmptyInstallDirectory -Path "$InstallDir\$($version.FullVersion)"
}
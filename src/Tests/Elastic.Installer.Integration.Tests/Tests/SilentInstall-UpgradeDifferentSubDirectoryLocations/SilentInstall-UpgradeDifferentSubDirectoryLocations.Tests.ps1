$currentDir = Split-Path -parent $MyInvocation.MyCommand.Path
Set-Location $currentDir

# mapped sync folder for common scripts
. $currentDir\..\common\Utils.ps1
. $currentDir\..\common\CommonTests.ps1
. $currentDir\..\common\Artifact.ps1

Get-Version
Get-PreviousVersions

$version = $Global:Version
$previousVersion = $Global:PreviousVersions[0]
$640Release = ConvertTo-Artifact "6.4.0"

# install data, config and logs as sub directories
$InstallDir = "D:\Elastic"
$DataDir = "D:\Elastic\$($previousVersion.FullVersion)\Data"
$ConfigDir = "D:\Elastic\$($previousVersion.FullVersion)\Config"
$LogsDir = "D:\Elastic\$($previousVersion.FullVersion)\Logs"

# install into different locations from the previous version
$UpgradedDataDir = "C:\Data"
$UpgradedConfigDir = "C:\Config"
$UpgradedLogsDir = "C:\Logs"

$tags = @('PreviousVersions', 'Plugins', 'SubDirectories') 

Describe -Name "Silent Install upgrade different sub directory locations install $($previousVersion.Description)" -Tags $tags {
	
	$v = $previousVersion.FullVersion
	$ExeArgs = "INSTALLDIR=$InstallDir\$v","DATADIRECTORY=$DataDir","CONFIGDIRECTORY=$ConfigDir","LOGSDIRECTORY=$LogsDir","PLUGINS=mapper-murmur3"

    Invoke-SilentInstall -Exeargs $ExeArgs -Version $previousVersion

    Context-ElasticsearchService

    Context-PingNode

    Context-EsHomeEnvironmentVariable -Expected "$InstallDir\$v\"

    Context-EsConfigEnvironmentVariable -Expected @{ 
		Version = $previousVersion
		Path = $ConfigDir
	}

    Context-PluginsInstalled -Expected @{ Plugins=@("mapper-murmur3") }

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
}

Describe -Name "Silent Install upgrade different sub directory locations from $($previousVersion.Description) to $($version.Description)" -Tags $tags {

	$v = $version.FullVersion
	$ExeArgs = "INSTALLDIR=$InstallDir\$v","DATADIRECTORY=$UpgradedDataDir","CONFIGDIRECTORY=$UpgradedConfigDir","LOGSDIRECTORY=$UpgradedLogsDir","PLUGINS=mapper-murmur3"

	# compiled MSI will fail when trying to upgrade from 
	# an installation that has config, logs, data in a sub
	# directory of a previous installation
	if ($version.Distribution -eq "Zip" -or (Compare-Artifact $version $640Release) -ge 0) {
		Context "Failed installation" {
			$exitCode = Invoke-SilentInstall -Exeargs $ExeArgs -Version $version -Upgrade

			It "Exit code is 1603" {
				$exitCode | Should Be 1603
			}
		}

		Copy-ElasticsearchLogToOut

		Context-EventContainsFailedInstallMessage -Version $v
	} 
	else {
		Invoke-SilentInstall -Exeargs $ExeArgs -Version $version -Upgrade

		Context-EsHomeEnvironmentVariable -Expected "$InstallDir\$v\"

		Context-EsConfigEnvironmentVariable -Expected @{ 
			Version = $version 
			Path = $UpgradedConfigDir
		}

		$expectedStatus = Get-ExpectedServiceStatus -Version $version -PreviousVersion $previousVersion

		Context-ElasticsearchService -Expected @{
			Status = $expectedStatus
		}

		Context-PingNode

		Context-PluginsInstalled -Expected @{ Plugins=@("mapper-murmur3") }

		Context-MsiRegistered

		Context-ServiceRunningUnderAccount -Expected "LocalSystem"

		Context-EmptyEventLog -Version $previousVersion

		Context-ClusterNameAndNodeName

		Context-ElasticsearchConfiguration -Expected @{
			Version = $version
			Data = $UpgradedDataDir
			Logs = $UpgradedLogsDir
		}

		Context-JvmOptions -Expected @{
			Version = $version
		}

		Copy-ElasticsearchLogToOut		
	}
}

Describe -Name "Silent Uninstall upgrade different sub directory locations uninstall $($version.Description)" -Tags $tags {

	# compiled MSI will fail when trying to upgrade from 
	# an installation that has config, logs, data in a sub
	# directory of a previous installation
	if ($version.Distribution -eq 'Zip') {
		Invoke-SilentUninstall -Version $previousVersion

		Context-NodeNotRunning

		Context-EsConfigEnvironmentVariableNull

		Context-EsHomeEnvironmentVariableNull

		Context-MsiNotRegistered

		Context-ElasticsearchServiceNotInstalled
	}
	else {
		$v = $version.FullVersion

		Invoke-SilentUninstall -Version $version

		Context-NodeNotRunning

		Context-EsConfigEnvironmentVariableNull

		Context-EsHomeEnvironmentVariableNull

		Context-MsiNotRegistered

		Context-ElasticsearchServiceNotInstalled

		Context-EmptyInstallDirectory -Path "$InstallDir\$($version.FullVersion)"

		Context-DirectoryExists -Path $ConfigDir -DeleteAfter
		Context-DirectoryExists -Path $DataDir -DeleteAfter
		Context-DirectoryExists -Path $LogsDir -DeleteAfter

		Context-DataDirectories -Path @($UpgradedConfigDir, $UpgradedDataDir, $UpgradedLogsDir) -DeleteAfter
	}
}
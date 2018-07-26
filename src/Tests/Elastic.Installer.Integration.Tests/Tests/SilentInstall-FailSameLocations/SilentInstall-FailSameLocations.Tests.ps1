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

Describe "Silent Expected Failed Install with same install locations $(($Global:Version).Description)" {
 	$InstallLocations = "INSTALLDIR=$InstallDir","DATADIRECTORY=$DataDir","CONFIGDIRECTORY=$ConfigDir","LOGSDIRECTORY=$LogsDir"
	$version = $Global:Version.FullVersion

	Context "Failed installation" {
		$exitCode = Invoke-SilentInstall -Exeargs $InstallLocations

		It "Exit code is 1603" {
			$exitCode | Should Be 1603
		}
	}

	Context-EventContainsFailedInstallMessage -Version $version

	Context-NodeNotRunning

	Context-EsConfigEnvironmentVariableNull

	Context-EsHomeEnvironmentVariableNull

	Context-MsiNotRegistered

	Context-ElasticsearchServiceNotInstalled

	$ProgramFiles = Get-ProgramFilesFolder
    $ExpectedHomeFolder = Join-Path -Path $ProgramFiles -ChildPath "Elastic\Elasticsearch\"

	Context-EmptyInstallDirectory -Path $ExpectedHomeFolder

	$configDirectory = "C:\ProgramData\Elastic\Elasticsearch\config"
	$dataDirectory = $configDirectory | Split-Path | Join-Path -ChildPath "data"
	$logsDirectory = $configDirectory | Split-Path | Join-Path -ChildPath "logs"

	Context-DirectoryNotExist -Path $configDirectory -DeleteAfter
	Context-DirectoryNotExist -Path $dataDirectory -DeleteAfter
	Context-DirectoryNotExist -Path $logsDirectory -DeleteAfter
}

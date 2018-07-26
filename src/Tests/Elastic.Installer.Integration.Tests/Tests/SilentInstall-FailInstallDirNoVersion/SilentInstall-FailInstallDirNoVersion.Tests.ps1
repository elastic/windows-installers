$currentDir = Split-Path -parent $MyInvocation.MyCommand.Path
Set-Location $currentDir

# mapped sync folder for common scripts
. $currentDir\..\common\Utils.ps1
. $currentDir\..\common\CommonTests.ps1
. $currentDir\..\common\SemVer.ps1

Get-Version
Get-PreviousVersions

Describe "Silent Failed Install with Installation directory not ending in version $(($Global:Version).Description)" {

	$version = $Global:Version.FullVersion

	Context "Failed installation" {
		$exitCode = Invoke-SilentInstall -Exeargs @("INSTALLDIR=C:\elastic")

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
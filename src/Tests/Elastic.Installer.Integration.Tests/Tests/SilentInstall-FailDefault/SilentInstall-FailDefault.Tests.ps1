$currentDir = Split-Path -parent $MyInvocation.MyCommand.Path
Set-Location $currentDir

# mapped sync folder for common scripts
. $currentDir\..\common\Utils.ps1
. $currentDir\..\common\CommonTests.ps1
. $currentDir\..\common\SemVer.ps1

Get-Version
Get-PreviousVersions

Describe "Silent Failed Install with default arguments" {

	$startDate = Get-Date
	$version = $Global:Version.FullVersion

	Context "Failed installation" {
		$exitCode = Invoke-SilentInstall -Exeargs @("WIXFAILWHENDEFERRED=1")

		It "Exit code is 1603" {
			$exitCode | Should Be 1603
		}
	}

	Context-EventContainsFailedInstallMessage -StartDate $startDate -Version $version

	Context-NodeNotRunning

	Context-EsConfigEnvironmentVariableNull

	Context-EsHomeEnvironmentVariableNull

	Context-MsiNotRegistered

	Context-ElasticsearchServiceNotInstalled

	$ProgramFiles = Get-ProgramFilesFolder
    $ExpectedHomeFolder = Join-Path -Path $ProgramFiles -ChildPath "Elastic\Elasticsearch\"

	Context-EmptyInstallDirectory -Path $ExpectedHomeFolder
}
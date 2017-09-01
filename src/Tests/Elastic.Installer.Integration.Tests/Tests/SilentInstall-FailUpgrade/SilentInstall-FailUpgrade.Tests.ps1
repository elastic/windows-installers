$currentDir = Split-Path -parent $MyInvocation.MyCommand.Path
Set-Location $currentDir

# mapped sync folder for common scripts
. $currentDir\..\common\Utils.ps1
. $currentDir\..\common\CommonTests.ps1

$credentials = "elastic:changeme"

Describe -Tag 'PreviousVersion' "Silent Install fail upgrade - Install previous version" {

	$previousVersion = $env:PreviousEsVersion
	
    Invoke-SilentInstall -Version $previousVersion

    Context-ElasticsearchService

    Context-PingNode -XPackSecurityInstalled $false

    $ProgramFiles = Get-ProgramFilesFolder
    $ExpectedHomeFolder = Join-Path -Path $ProgramFiles -ChildPath "Elastic\Elasticsearch\"

    Context-EsHomeEnvironmentVariable -Expected $ExpectedHomeFolder

    $ProfileFolder = $env:ALLUSERSPROFILE
    $ExpectedConfigFolder = Join-Path -Path $ProfileFolder -ChildPath "Elastic\Elasticsearch\config"

    Context-EsConfigEnvironmentVariable -Expected $ExpectedConfigFolder

	Context-PluginsInstalled

    Context-MsiRegistered -Expected @{
		Name = "Elasticsearch $previousVersion"
		Caption = "Elasticsearch $previousVersion"
		Version = $previousVersion
	}

    Context-ServiceRunningUnderAccount -Expected "LocalSystem"

    Context-EmptyEventLog

	Context-ClusterNameAndNodeName

    Context-ElasticsearchConfiguration

    Context-JvmOptions

	Context-InsertData
}

Describe -Tag 'PreviousVersion' "Silent Install fail upgrade - Fail when Upgrading" {

	$version = $env:EsVersion
	$startDate = Get-Date

	Context "Failed installation" {
		$exitCode = Invoke-SilentInstall -Exeargs @("WIXFAILWHENDEFERRED=1") -Version $version

		It "Exit code is 1603" {
			$exitCode | Should Be 1603
		}
	}

	Context-EventContainsFailedInstallMessage -StartDate $startDate -Version $version

	# Existing version should still be installed and running
	# NOTE: It may be in StartingPending to begin, after failed upgrade
	Context "Elasticsearch service" {	
		$service = Get-ElasticsearchService

		It "Service is not null" {
            $Service | Should Not Be $null
        }

		if ($service.Status -ne "Running") {
			$service.Refresh()
			$startTime = Get-Date
			$timeout = New-TimeSpan -Seconds 30

			while ($service.Status -ne "Running") {
				if ($(Get-Date) - $startTime -gt $timeout) {
					throw "Attempted to start the service in $timeout, but did not start"
				}

				Start-Sleep -m 250
				$service.Refresh()
			}
		}
	}

    Context-ElasticsearchService

    Context-PingNode -XPackSecurityInstalled $false

    $ProgramFiles = Get-ProgramFilesFolder
    $ExpectedHomeFolder = Join-Path -Path $ProgramFiles -ChildPath "Elastic\Elasticsearch\"

    Context-EsHomeEnvironmentVariable -Expected $ExpectedHomeFolder

    $ProfileFolder = $env:ALLUSERSPROFILE
    $ExpectedConfigFolder = Join-Path -Path $ProfileFolder -ChildPath "Elastic\Elasticsearch\config"

    Context-EsConfigEnvironmentVariable -Expected $ExpectedConfigFolder

    Context-PluginsInstalled

	# previous version still installed
    Context-MsiRegistered -Expected @{
		Name = "Elasticsearch $previousVersion"
		Caption = "Elasticsearch $previousVersion"
		Version = $previousVersion
	}

    Context-ServiceRunningUnderAccount -Expected "LocalSystem"

	Context-ClusterNameAndNodeName

    Context-ElasticsearchConfiguration

    Context-JvmOptions

	# Check inserted data still exists
	Context-ReadData
}

Describe -Tag 'PreviousVersion' "Silent Uninstall fail upgrade - Uninstall old version" {

	$version = $env:PreviousEsVersion

    Invoke-SilentUninstall -Version $version

	Context-NodeNotRunning

	Context-EnvironmentVariableNull -Name "CONF_DIR"

	Context-EnvironmentVariableNull -Name "ES_HOME"

	Context-MsiNotRegistered

	Context-ElasticsearchServiceNotInstalled

 	$ProgramFiles = Get-ProgramFilesFolder
    $ExpectedHomeFolder = Join-Path -Path $ProgramFiles -ChildPath "Elastic\Elasticsearch\"

	Context-EmptyInstallDirectory -Path $ExpectedHomeFolder
}

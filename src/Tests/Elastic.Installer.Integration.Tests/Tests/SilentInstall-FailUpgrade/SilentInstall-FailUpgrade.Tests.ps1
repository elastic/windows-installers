$currentDir = Split-Path -parent $MyInvocation.MyCommand.Path
Set-Location $currentDir

# mapped sync folder for common scripts
. $currentDir\..\common\Utils.ps1
. $currentDir\..\common\CommonTests.ps1
. $currentDir\..\common\SemVer.ps1

$credentials = "elastic:changeme"

Get-Version
Get-PreviousVersions

$version = $Global:Version
$previousVersion = $Global:PreviousVersions[0]

Describe -Tag 'PreviousVersions' "Silent Install fail upgrade - Install previous version $($previousVersion.Description)" {

	$v = $previousVersion.FullVersion
	
    Invoke-SilentInstall -Version $previousVersion

    Context-ElasticsearchService

    Context-PingNode -XPackSecurityInstalled $false

    $ProgramFiles = Get-ProgramFilesFolder
    $ExpectedHomeFolder = Join-Path -Path $ProgramFiles -ChildPath "Elastic\Elasticsearch\"

    Context-EsHomeEnvironmentVariable -Expected $ExpectedHomeFolder

    $ProfileFolder = $env:ALLUSERSPROFILE
    $ExpectedConfigFolder = Join-Path -Path $ProfileFolder -ChildPath "Elastic\Elasticsearch\config"

    Context-EsConfigEnvironmentVariable -Expected @{ 
		Version = $previousVersion
		Path = $ExpectedConfigFolder
	}

	Context-PluginsInstalled

    Context-MsiRegistered -Expected @{
		Name = "Elasticsearch $v"
		Caption = "Elasticsearch $v"
		Version = "$($previousVersion.Major).$($previousVersion.Minor).$($previousVersion.Patch)"
	}

    Context-ServiceRunningUnderAccount -Expected "LocalSystem"

    Context-EmptyEventLog

	Context-ClusterNameAndNodeName

    Context-ElasticsearchConfiguration -Expected @{
		Version = $previousVersion
	}

    Context-JvmOptions -Expected @{
		Version = $previousVersion
	}

	Context-InsertData
}

Describe -Tag 'PreviousVersions' "Silent Install fail upgrade - Fail when upgrading to $($version.Description)" {

	$v = $version.FullVersion
	$pv = $previousVersion.FullVersion
	$startDate = Get-Date

	Context "Failed installation" {
		$exitCode = Invoke-SilentInstall -Exeargs @("WIXFAILWHENDEFERRED=1") -Version $version -Upgrade

		It "Exit code is 1603" {
			$exitCode | Should Be 1603
		}
	}

	Copy-ElasticsearchLogToOut

	Context-EventContainsFailedInstallMessage -StartDate $startDate -Version $v

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

    Context-EsConfigEnvironmentVariable -Expected @{ 
		Version = $previousVersion
		Path = $ExpectedConfigFolder
	}

    Context-PluginsInstalled

	# previous version still installed
    Context-MsiRegistered -Expected @{
		Name = "Elasticsearch $pv"
		Caption = "Elasticsearch $pv"
		Version = "$($previousVersion.Major).$($previousVersion.Minor).$($previousVersion.Patch)"
	}

    Context-ServiceRunningUnderAccount -Expected "LocalSystem"

	Context-ClusterNameAndNodeName

    Context-ElasticsearchConfiguration -Expected @{
		Version = $previousVersion
	}

    Context-JvmOptions -Expected @{
		Version = $previousVersion
	}

	# Check inserted data still exists
	Context-ReadData
}

Describe -Tag 'PreviousVersions' "Silent Uninstall fail upgrade - Uninstall $($previousVersion.Description)" {

	$v = $previousVersion.FullVersion

    Invoke-SilentUninstall -Version $previousVersion

	Context-NodeNotRunning

	Context-EsConfigEnvironmentVariableNull -Version $v

	Context-EsHomeEnvironmentVariableNull

	Context-MsiNotRegistered

	Context-ElasticsearchServiceNotInstalled

 	$ProgramFiles = Get-ProgramFilesFolder
    $ExpectedHomeFolder = Join-Path -Path $ProgramFiles -ChildPath "Elastic\Elasticsearch\"

	Context-EmptyInstallDirectory -Path $ExpectedHomeFolder
}
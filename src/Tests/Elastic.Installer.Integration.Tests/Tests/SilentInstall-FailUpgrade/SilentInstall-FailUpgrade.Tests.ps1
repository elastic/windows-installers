$currentDir = Split-Path -parent $MyInvocation.MyCommand.Path
Set-Location $currentDir

# mapped sync folder for common scripts
. $currentDir\..\common\Utils.ps1
. $currentDir\..\common\CommonTests.ps1
. $currentDir\..\common\Artifact.ps1

$credentials = "elastic:changeme"

Get-Version
Get-PreviousVersions

$version = $Global:Version
$previousVersion = $Global:PreviousVersions[0]
$tags = @('PreviousVersions')

Describe -Name "Silent Install fail upgrade install $($previousVersion.Description)" -Tags $tags {

	$v = $previousVersion.FullVersion
	
    Invoke-SilentInstall -Version $previousVersion

    Context-ElasticsearchService

    Context-PingNode

    $ProgramFiles = Get-ProgramFilesFolder
	$ChildPath = Get-ChildPath -Version $previousVersion
    $ExpectedHomeFolder = Join-Path -Path $ProgramFiles -ChildPath $ChildPath

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

    Context-EmptyEventLog -Version $previousVersion

	Context-ClusterNameAndNodeName

    Context-ElasticsearchConfiguration -Expected @{
		Version = $previousVersion
	}

    Context-JvmOptions -Expected @{
		Version = $previousVersion
	}
}

Describe -Name "Silent Install fail upgrade fail to $($version.Description)" -Tags $tags {

	$v = $version.FullVersion
	$pv = $previousVersion.FullVersion

	Context "Failed installation" {
		$exitCode = Invoke-SilentInstall -Exeargs @("WIXFAILWHENDEFERRED=1") -Version $version -Upgrade

		It "Exit code is 1603" {
			$exitCode | Should Be 1603
		}
	}

	Copy-ElasticsearchLogToOut

	Context-EventContainsFailedInstallMessage -Version $v

	# Existing version should still be installed and running
	# NOTE: It may be in StartingPending to begin, after failed upgrade
	Context "Elasticsearch service" {	
		$service = Get-ElasticsearchService

		It "Service is not null" {
            $Service | Should Not Be $null
        }
	}

    $ProgramFiles = Get-ProgramFilesFolder
	$ChildPath = Get-ChildPath $previousVersion
    $ExpectedHomeFolder = Join-Path -Path $ProgramFiles -ChildPath $ChildPath

    Context-EsHomeEnvironmentVariable -Expected $ExpectedHomeFolder

    $ProfileFolder = $env:ALLUSERSPROFILE
    $ExpectedConfigFolder = Join-Path -Path $ProfileFolder -ChildPath "Elastic\Elasticsearch\config"

    Context-EsConfigEnvironmentVariable -Expected @{ 
		Version = $previousVersion
		Path = $ExpectedConfigFolder
	}

	$dataDirectory = $ExpectedConfigFolder | Split-Path | Join-Path -ChildPath "data"
	$logsDirectory = $ExpectedConfigFolder | Split-Path | Join-Path -ChildPath "logs"
	Context-DirectoryExists -Path $ExpectedConfigFolder 
	Context-DirectoryExists -Path $dataDirectory 
	Context-DirectoryExists -Path $logsDirectory

    Context-PluginsInstalled

	# previous version still installed
    Context-MsiRegistered -Expected @{
		Name = "Elasticsearch $pv"
		Caption = "Elasticsearch $pv"
		Version = "$($previousVersion.Major).$($previousVersion.Minor).$($previousVersion.Patch)"
	}

    Context-ServiceRunningUnderAccount -Expected "LocalSystem"

    Context-ElasticsearchConfiguration -Expected @{
		Version = $previousVersion
	}

    Context-JvmOptions -Expected @{
		Version = $previousVersion
	}
}

Describe -Name "Silent Uninstall fail upgrade uninstall $($previousVersion.Description)" -Tags $tags {

	$configDirectory = Get-ConfigEnvironmentVariableForVersion -Version $previousVersion | Get-MachineEnvironmentVariable
	$dataDirectory = $configDirectory | Split-Path | Join-Path -ChildPath "data"
	$logsDirectory = $configDirectory | Split-Path | Join-Path -ChildPath "logs"

	$v = $previousVersion.FullVersion

    Invoke-SilentUninstall -Version $previousVersion

	Context-NodeNotRunning

	Context-EsConfigEnvironmentVariableNull -Version $v

	Context-EsHomeEnvironmentVariableNull

	Context-MsiNotRegistered

	Context-ElasticsearchServiceNotInstalled

	Context-EmptyInstallDirectory

	Context-DataDirectories -Version $previousVersion -Path @($configDirectory, $dataDirectory, $logsDirectory) -DeleteAfter
}
$currentDir = Split-Path -parent $MyInvocation.MyCommand.Path
Set-Location $currentDir

# mapped sync folder for common scripts
. $currentDir\..\common\Utils.ps1
. $currentDir\..\common\CommonTests.ps1
. $currentDir\..\common\SemVer.ps1

$version = $Global:Version
$previousVersion = $Global:PreviousVersions[0]

Describe -Tag 'PreviousVersion' "Silent Install upgrade - Install previous version $($previousVersion.Description)" {

	$v = $previousVersion.FullVersion

    Invoke-SilentInstall -Version $v

    Context-ElasticsearchService

    Context-PingNode -XPackSecurityInstalled $false

    $ProgramFiles = Get-ProgramFilesFolder
    $ExpectedHomeFolder = Join-Path -Path $ProgramFiles -ChildPath "Elastic\Elasticsearch\"

    Context-EsHomeEnvironmentVariable -Expected $ExpectedHomeFolder

    $ProfileFolder = $env:ALLUSERSPROFILE
    $ExpectedConfigFolder = Join-Path -Path $ProfileFolder -ChildPath "Elastic\Elasticsearch\config"

    Context-EsConfigEnvironmentVariable -Expected @{ 
		Version = $v 
		Path = $ExpectedConfigFolder
	}

    Context-PluginsInstalled

    Context-MsiRegistered -Expected @{
		Name = "Elasticsearch $v"
		Caption = "Elasticsearch $v"
		Version = $v
	}

    Context-ServiceRunningUnderAccount -Expected "LocalSystem"

    Context-EmptyEventLog

	Context-ClusterNameAndNodeName

    Context-ElasticsearchConfiguration -Expected @{
		Version = $v
	}

    Context-JvmOptions -Expected @{
		Version = $v
	}

	# Insert some data
	Context-InsertData
}

Describe -Tag 'PreviousVersions' "Silent Install upgrade - Upgrade from $($previousVersion.Description) to $($version.Description)" {

	$v = $version.FullVersion

    Invoke-SilentInstall -Version $v

	$ProgramFiles = Get-ProgramFilesFolder
    $ExpectedHomeFolder = Join-Path -Path $ProgramFiles -ChildPath "Elastic\Elasticsearch\"

    Context-EsHomeEnvironmentVariable -Expected $ExpectedHomeFolder

    $ProfileFolder = $env:ALLUSERSPROFILE
    $ExpectedConfigFolder = Join-Path -Path $ProfileFolder -ChildPath "Elastic\Elasticsearch\config"

    Context-EsConfigEnvironmentVariable -Expected @{ 
		Version = $v 
		Path = $ExpectedConfigFolder
	}

	$expectedStatus = Get-ExpectedServiceStatus -Version $version -PreviousVersion $previousVersion

    Context-ElasticsearchService -Expected @{
		Status = $expectedStatus
	}

    Context-PingNode -XPackSecurityInstalled $false

    Context-PluginsInstalled

    Context-MsiRegistered

    Context-ServiceRunningUnderAccount -Expected "LocalSystem"

    Context-EmptyEventLog

	Context-ClusterNameAndNodeName

    Context-ElasticsearchConfiguration -Expected @{
		Version = $v
	}

    Context-JvmOptions -Expected @{
		Version = $v
	}

	# Check inserted data still exists
	Context-ReadData
}

Describe -Tag 'PreviousVersion' "Silent Uninstall upgrade - Uninstall new version $($version.Description)" {

	$v = $version.FullVersion

    Invoke-SilentUninstall -Version $v

	Context-NodeNotRunning

	Context-EsConfigEnvironmentVariableNull

	Context-EsHomeEnvironmentVariableNull

	Context-MsiNotRegistered

	Context-ElasticsearchServiceNotInstalled

	$ProgramFiles = Get-ProgramFilesFolder
    $ExpectedHomeFolder = Join-Path -Path $ProgramFiles -ChildPath "Elastic\Elasticsearch\"

	Context-EmptyInstallDirectory -Path $ExpectedHomeFolder
}

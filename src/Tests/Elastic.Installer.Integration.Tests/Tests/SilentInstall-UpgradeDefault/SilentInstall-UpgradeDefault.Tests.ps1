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

Describe -Tag 'PreviousVersions' "Silent Install upgrade - Install previous version $($previousVersion.Description)" {

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

	# Insert some data
	Context-InsertData
}

Describe -Tag 'PreviousVersions' "Silent Install upgrade - Upgrade from $($previousVersion.Description) to $($version.Description)" {

	$v = $version.FullVersion

    Invoke-SilentInstall -Version $v -Upgrade

	$ProgramFiles = Get-ProgramFilesFolder
    $ExpectedHomeFolder = Join-Path -Path $ProgramFiles -ChildPath "Elastic\Elasticsearch\"

    Context-EsHomeEnvironmentVariable -Expected $ExpectedHomeFolder

    $ProfileFolder = $env:ALLUSERSPROFILE
    $ExpectedConfigFolder = Join-Path -Path $ProfileFolder -ChildPath "Elastic\Elasticsearch\config"

    Context-EsConfigEnvironmentVariable -Expected @{ 
		Version = $version 
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
		Version = $version
	}

    Context-JvmOptions -Expected @{
		Version = $version
	}

	# Check inserted data still exists
	Context-ReadData

	Copy-ElasticsearchLogToOut
}

Describe -Tag 'PreviousVersions' "Silent Uninstall upgrade - Uninstall new version $($version.Description)" {

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
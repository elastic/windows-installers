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

Describe -Tag 'PreviousVersions' "Silent Install upgrade with plugins - Install previous version $($previousVersion.Description)" {

	$v = $previousVersion.FullVersion
	$ExeArgs = @("PLUGINS=x-pack,ingest-geoip,ingest-attachment")

	# set bootstrap password and x-pack security
	if ($version.Major -ge 6) {
		$ExeArgs = $ExeArgs + @("BOOTSTRAPPASSWORD=changeme","XPACKSECURITYENABLED=true") 
	}

    Invoke-SilentInstall -Exeargs $ExeArgs -Version $previousVersion

    Context-ElasticsearchService

    Context-PingNode -XPackSecurityInstalled

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

    Context-PluginsInstalled -Expected @{ Plugins=@("x-pack","ingest-geoip","ingest-attachment") }

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
	}

    Context-JvmOptions -Expected @{
		Version = $previousVersion
	}

	# Insert some data
	Context-InsertData -Credentials $credentials
}

Describe -Tag 'PreviousVersions' "Silent Install upgrade with plugins - Upgrade from $($previousVersion.Description) to $($version.Description)" {

	$v = $version.FullVersion

	$ExeArgs = @("PLUGINS=x-pack,ingest-geoip,ingest-attachment")

	# set bootstrap password and x-pack security
	if ($version.Major -ge 6) {
		$ExeArgs = $ExeArgs + @("BOOTSTRAPPASSWORD=changeme","XPACKSECURITYENABLED=true") 
	}

    Invoke-SilentInstall -Exeargs $ExeArgs -Version $version -Upgrade

    $ProgramFiles = Get-ProgramFilesFolder
	$ChildPath = Get-ChildPath $version
    $ExpectedHomeFolder = Join-Path -Path $ProgramFiles -ChildPath $ChildPath

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

	Context-PingNode -XPackSecurityInstalled

    Context-PluginsInstalled -Expected @{ Plugins=@("x-pack","ingest-geoip","ingest-attachment") }

    Context-MsiRegistered

    Context-ServiceRunningUnderAccount -Expected "LocalSystem"

	if ((Compare-SemanticVersion $previousVersion $(ConvertTo-SemanticVersion "6.0.0") -le 0) `
		-and $previousVersion.SourceType -ne "Compile") {
		# TODO: event log may contain events similar to:
		#
		# System.ComponentModel.Win32Exception (0x80004005): The system cannot find the file specified
		# when running Cleanup action in the old installer uninstall process, 
		# because the old install plugin script no longer exists.
	}
	else {
    Context-EmptyEventLog
	}

	Context-ClusterNameAndNodeName -Expected @{ Credentials = $credentials }

    Context-ElasticsearchConfiguration -Expected @{
		Version = $version
	}

    Context-JvmOptions -Expected @{
		Version = $version
	}

	# Check inserted data still exists
	Context-ReadData -Credentials $credentials
	
	Copy-ElasticsearchLogToOut
}

Describe -Tag 'PreviousVersions' "Silent Uninstall upgrade with plugins - Uninstall $($version.Description)" {

	$v = $version.FullVersion

    Invoke-SilentUninstall -Version $version

	Context-NodeNotRunning

	Context-EsConfigEnvironmentVariableNull

	Context-EsHomeEnvironmentVariableNull

	Context-MsiNotRegistered

	Context-ElasticsearchServiceNotInstalled

	$ProgramFiles = Get-ProgramFilesFolder
	$ChildPath = Get-ChildPath $version
    $ExpectedHomeFolder = Join-Path -Path $ProgramFiles -ChildPath $ChildPath

	Context-EmptyInstallDirectory -Path $ExpectedHomeFolder
}
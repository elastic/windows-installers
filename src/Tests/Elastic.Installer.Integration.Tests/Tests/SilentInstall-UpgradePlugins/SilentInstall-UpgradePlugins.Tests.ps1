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
$tags = @('PreviousVersions', 'XPack') 

Describe -Name "Silent Install upgrade with plugins install $($previousVersion.Description)" -Tags $tags {

	$v = $previousVersion.FullVersion

	# don't try to install X-Pack for 6.3.0+
	$630Release = ConvertTo-SemanticVersion "6.3.0"
	if ((Compare-SemanticVersion $previousVersion $630Release) -ge 0) {
		$plugins = "ingest-geoip,ingest-attachment"
	} else {
		$plugins = "x-pack,ingest-geoip,ingest-attachment"
	}

	$ExeArgs = @("PLUGINS=$plugins")

	# set bootstrap password and x-pack security
	if ($version.Major -ge 6) {
		$ExeArgs = $ExeArgs + @(
					"BOOTSTRAPPASSWORD=changeme"
					"XPACKSECURITYENABLED=true"
					"XPACKLICENSE=Trial"
					"SKIPSETTINGPASSWORDS=true")
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

    Context-PluginsInstalled -Expected @{ Plugins=($plugins -split ",") }

    Context-MsiRegistered -Expected @{
		Name = "Elasticsearch $v"
		Caption = "Elasticsearch $v"
		Version = "$($previousVersion.Major).$($previousVersion.Minor).$($previousVersion.Patch)"
	}

    Context-ServiceRunningUnderAccount -Expected "LocalSystem"

    Context-EmptyEventLog -Version $previousVersion

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

Describe -Name "Silent Install upgrade with plugins from $($previousVersion.Description) to $($version.Description)" -Tags $tags {

	$v = $version.FullVersion

	# don't try to install X-Pack for 6.3.0+
	$630Release = ConvertTo-SemanticVersion "6.3.0"
	if ((Compare-SemanticVersion $version $630Release) -ge 0) {
		$plugins = "ingest-geoip,ingest-attachment"
	} else {
		$plugins = "x-pack,ingest-geoip,ingest-attachment"
	}

	$ExeArgs = @("PLUGINS=$plugins")

	# set bootstrap password and x-pack security
	if ($version.Major -ge 6) {
		$ExeArgs = $ExeArgs + @(
			"BOOTSTRAPPASSWORD=changeme"
			"XPACKSECURITYENABLED=true"
			"XPACKLICENSE=Trial"
			"SKIPSETTINGPASSWORDS=true")
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

    Context-PluginsInstalled -Expected @{ Plugins=($plugins -split ",") }

    Context-MsiRegistered

    Context-ServiceRunningUnderAccount -Expected "LocalSystem"

	Context-EmptyEventLog -Version $previousVersion

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

Describe -Tag 'PreviousVersions' "Silent Uninstall upgrade with plugins uninstall $($version.Description)" {

	$configDirectory = Get-ConfigEnvironmentVariableForVersion | Get-MachineEnvironmentVariable
	$dataDirectory = $configDirectory | Split-Path | Join-Path -ChildPath "data"
	$logsDirectory = $configDirectory | Split-Path | Join-Path -ChildPath "logs"

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

	Context-DataDirectories -Path @($configDirectory, $dataDirectory, $logsDirectory) -DeleteAfter
}
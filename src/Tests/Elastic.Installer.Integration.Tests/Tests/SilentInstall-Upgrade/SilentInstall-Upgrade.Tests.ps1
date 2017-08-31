$currentDir = Split-Path -parent $MyInvocation.MyCommand.Path
Set-Location $currentDir

# mapped sync folder for common scripts
. $currentDir\..\common\Utils.ps1
. $currentDir\..\common\CommonTests.ps1

$credentials = "elastic:changeme"

Describe -Tag 'PreviousVersion' "Silent Install upgrade - Install previous version" {

	$previousVersion = $env:PreviousEsVersion
	
    Invoke-SilentInstall -Exeargs @("PLUGINS=x-pack,ingest-geoip,ingest-attachment") -Version $previousVersion

    Context-ElasticsearchService

    Context-PingNode -XPackSecurityInstalled $true

    $ProgramFiles = Get-ProgramFilesFolder
    $ExpectedHomeFolder = Join-Path -Path $ProgramFiles -ChildPath "Elastic\Elasticsearch\"

    Context-EsHomeEnvironmentVariable -Expected $ExpectedHomeFolder

    $ProfileFolder = $env:ALLUSERSPROFILE
    $ExpectedConfigFolder = Join-Path -Path $ProfileFolder -ChildPath "Elastic\Elasticsearch\config"

    Context-EsConfigEnvironmentVariable -Expected $ExpectedConfigFolder

    Context-PluginsInstalled -Expected @{ Plugins=@("x-pack","ingest-geoip","ingest-attachment") }

    Context-MsiRegistered -Expected @{
		Name = "Elasticsearch $previousVersion"
		Caption = "Elasticsearch $previousVersion"
		Version = $previousVersion
	}

    Context-ServiceRunningUnderAccount -Expected "LocalSystem"

    Context-EmptyEventLog

	Context-ClusterNameAndNodeName -Expected @{ Credentials = $credentials }

    Context-ElasticsearchConfiguration

    Context-JvmOptions

	# Insert some data
	Context-InsertData -Credentials "elastic:changeme"
}

Describe -Tag 'PreviousVersion' "Silent Install upgrade - Upgrade to new version" {

	$version = $env:EsVersion

    Invoke-SilentInstall -Exeargs @("PLUGINS=x-pack,ingest-geoip,ingest-attachment") -Version $version

	# BUG: Service is not started *after* upgrade, start manually for now
	Context "Elasticsearch service" {	
		$service = Get-ElasticsearchService

		It "Service is not null" {
            $Service | Should Not Be $null
        }

		if ($service.Status -ne "Running") {
			$service.Start()
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

    Context-PingNode -XPackSecurityInstalled $true

    $ProgramFiles = Get-ProgramFilesFolder
    $ExpectedHomeFolder = Join-Path -Path $ProgramFiles -ChildPath "Elastic\Elasticsearch\"

    Context-EsHomeEnvironmentVariable -Expected $ExpectedHomeFolder

    $ProfileFolder = $env:ALLUSERSPROFILE
    $ExpectedConfigFolder = Join-Path -Path $ProfileFolder -ChildPath "Elastic\Elasticsearch\config"

    Context-EsConfigEnvironmentVariable -Expected $ExpectedConfigFolder

    Context-PluginsInstalled -Expected @{ Plugins=@("x-pack","ingest-geoip","ingest-attachment") }

    Context-MsiRegistered

    Context-ServiceRunningUnderAccount -Expected "LocalSystem"

    Context-EmptyEventLog

	Context-ClusterNameAndNodeName -Expected @{ Credentials = $credentials }

    Context-ElasticsearchConfiguration

    Context-JvmOptions

	# Check inserted data still exists
	Context-ReadData -Credentials $credentials
}

Describe -Tag 'PreviousVersion' "Silent Uninstall upgrade - Uninstall new version" {

	$version = $env:EsVersion

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

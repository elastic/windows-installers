$currentDir = Split-Path -parent $MyInvocation.MyCommand.Path
Set-Location $currentDir

# mapped sync folder for common scripts
. $currentDir\..\common\Utils.ps1
. $currentDir\..\common\CommonTests.ps1

Describe "Silent Install with x-pack, ingest-geoip and ingest-attachment plugins" {

    Invoke-SilentInstall -Exeargs @("PLUGINS=x-pack,ingest-geoip,ingest-attachment")

    Context-PingNode -XPackSecurityInstalled $true

    Context-PluginsInstalled -Expected @{ Plugins=@("x-pack","ingest-geoip","ingest-attachment") }

    Context-ClusterNameAndNodeName -Expected @{ Credentials = "elastic:changeme" }
}

Describe "Silent Uninstall with x-pack, ingest-geoip and ingest-attachment plugins" {

    Invoke-SilentUninstall

	Context-NodeNotRunning

	Context-EnvironmentVariableNull -Name "CONF_DIR"

	Context-EnvironmentVariableNull -Name "ES_HOME"

	Context-MsiNotRegistered

	Context-ElasticsearchServiceNotInstalled

	$ProgramFiles = Get-ProgramFilesFolder
    $ExpectedHomeFolder = Join-Path -Path $ProgramFiles -ChildPath "Elastic\Elasticsearch\"

	Context-EmptyInstallDirectory -Path $ExpectedHomeFolder
}
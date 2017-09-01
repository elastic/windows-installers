$currentDir = Split-Path -parent $MyInvocation.MyCommand.Path
Set-Location $currentDir

# mapped sync folder for common scripts
. $currentDir\..\common\Utils.ps1
. $currentDir\..\common\CommonTests.ps1

Describe "Silent Install with default arguments" {

    Invoke-SilentInstall

    Context-ElasticsearchService

    Context-PingNode -XPackSecurityInstalled $false

    $ProgramFiles = Get-ProgramFilesFolder
    $ExpectedHomeFolder = Join-Path -Path $ProgramFiles -ChildPath "Elastic\Elasticsearch\"

    Context-EsHomeEnvironmentVariable -Expected $ExpectedHomeFolder

    $ProfileFolder = $env:ALLUSERSPROFILE
    $ExpectedConfigFolder = Join-Path -Path $ProfileFolder -ChildPath "Elastic\Elasticsearch\config"

    Context-EsConfigEnvironmentVariable -Expected $ExpectedConfigFolder

    Context-PluginsInstalled

    Context-MsiRegistered

    Context-ServiceRunningUnderAccount -Expected "LocalSystem"

    Context-EmptyEventLog

	Context-ClusterNameAndNodeName

    Context-ElasticsearchConfiguration

    Context-JvmOptions
}

Describe "Silent Uninstall" {

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

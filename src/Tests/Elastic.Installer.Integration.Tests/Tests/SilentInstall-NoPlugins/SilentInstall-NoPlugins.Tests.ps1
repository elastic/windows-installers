$currentDir = Split-Path -parent $MyInvocation.MyCommand.Path
Set-Location $currentDir

# mapped sync folder for common scripts
. $currentDir\..\common\Utils.ps1
. $currentDir\..\common\CommonTests.ps1
. $currentDir\..\common\SemVer.ps1

Describe "Silent Install with no plugins" {

    Invoke-SilentInstall -Exeargs @("PLUGINS=")

    Context-PingNode -XPackSecurityInstalled $false

    Context-PluginsInstalled -Expected @{ Plugins=@() }

    Context-ClusterNameAndNodeName
}

Describe "Silent Uninstall with no plugins" {

    Invoke-SilentUninstall

	Context-NodeNotRunning

	Context-EsConfigEnvironmentVariableNull

	Context-EsHomeEnvironmentVariableNull

	Context-MsiNotRegistered

	Context-ElasticsearchServiceNotInstalled

	$ProgramFiles = Get-ProgramFilesFolder
    $ExpectedHomeFolder = Join-Path -Path $ProgramFiles -ChildPath "Elastic\Elasticsearch\"

	Context-EmptyInstallDirectory -Path $ExpectedHomeFolder
}
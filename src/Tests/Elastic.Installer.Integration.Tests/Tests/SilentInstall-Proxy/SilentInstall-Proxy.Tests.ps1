$currentDir = Split-Path -parent $MyInvocation.MyCommand.Path
Set-Location $currentDir

# mapped sync folder for common scripts
. $currentDir\..\common\Utils.ps1
. $currentDir\..\common\CommonTests.ps1
. $currentDir\..\common\SemVer.ps1

Get-Version
Get-PreviousVersions

Describe "Silent Install x-pack through HTTPS proxy" {
	$port = 8888
	Start-Fiddler -Port $port

    Invoke-SilentInstall -Exeargs @("PLUGINS=x-pack","HTTPSPROXYHOST=localhost", "HTTPSPROXYPORT=$port")

    Context-PingNode -XPackSecurityInstalled $true

    Context-PluginsInstalled -Expected @{ Plugins=@("x-pack") }

    Context-ClusterNameAndNodeName -Expected @{ Credentials = "elastic:changeme" }

	Context-FiddlerSessionContainsEntry

	Stop-Fiddler
}

Describe "Silent Uninstall with x-pack through HTTPS proxy" {

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
$currentDir = Split-Path -parent $MyInvocation.MyCommand.Path
Set-Location $currentDir

# mapped sync folder for common scripts
. $currentDir\..\common\Utils.ps1
. $currentDir\..\common\CommonTests.ps1
. $currentDir\..\common\SemVer.ps1

Get-Version
Get-PreviousVersions

Describe "Silent Install x-pack through HTTPS proxy $(($Global:Version).Description)" {
	$port = 8888
	Start-Fiddler -Port $port

	$exeArgs = @(
		"PLUGINS=x-pack",
		"HTTPSPROXYHOST=localhost", 
		"HTTPSPROXYPORT=$port",
		"XPACKSECURITYENABLED=true", 
		"XPACKLICENSE=Trial", 
		"SKIPSETTINGPASSWORDS=true",
		"BOOTSTRAPPASSWORD=changeme")

    Invoke-SilentInstall -Exeargs $exeArgs

    Context-PingNode -XPackSecurityInstalled

    Context-PluginsInstalled -Expected @{ Plugins=@("x-pack") }

    Context-ClusterNameAndNodeName -Expected @{ Credentials = "elastic:changeme" }

	Context-FiddlerSessionContainsEntry

	Stop-Fiddler

	Copy-ElasticsearchLogToOut
}

Describe "Silent Uninstall x-pack through HTTPS proxy $(($Global:Version).Description)" {

    Invoke-SilentUninstall

	Context-NodeNotRunning

	Context-EsConfigEnvironmentVariableNull

	Context-EsHomeEnvironmentVariableNull

	Context-MsiNotRegistered

	Context-ElasticsearchServiceNotInstalled

	$ProgramFiles = Get-ProgramFilesFolder
	$ChildPath = Get-ChildPath
    $ExpectedHomeFolder = Join-Path -Path $ProgramFiles -ChildPath $ChildPath

	Context-EmptyInstallDirectory -Path $ExpectedHomeFolder
}
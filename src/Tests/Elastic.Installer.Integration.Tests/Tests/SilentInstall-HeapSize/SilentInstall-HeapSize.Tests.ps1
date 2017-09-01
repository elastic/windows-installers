$currentDir = Split-Path -parent $MyInvocation.MyCommand.Path
Set-Location $currentDir

# mapped sync folder for common scripts
. $currentDir\..\common\Utils.ps1
. $currentDir\..\common\CommonTests.ps1

Describe "Silent Install with 1024mb heap size" {
    $HeapSize = 1024

    Invoke-SilentInstall @(,"SELECTEDMEMORY=$HeapSize")

    Context-PingNode -XPackSecurityInstalled $false
    Context-JvmOptions -Expected 1024

    Invoke-SilentUninstall
}

Describe "Silent Uninstall with 1024mb heap size" {

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
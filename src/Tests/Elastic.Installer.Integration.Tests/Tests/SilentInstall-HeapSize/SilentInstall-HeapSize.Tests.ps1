$currentDir = Split-Path -parent $MyInvocation.MyCommand.Path
Set-Location $currentDir

# mapped sync folder for common scripts
. $currentDir\..\common\Utils.ps1
. $currentDir\..\common\CommonTests.ps1

Describe "Silent Install with 1024mb heap size" {
    $HeapSize = 1024

    Invoke-SilentInstall @(,"SELECTEDMEMORY=$HeapSize")

    Context-PingNode -ShieldInstalled $true
    Context-JvmOptions -Expected 1024

    Invoke-SilentUninstall
}

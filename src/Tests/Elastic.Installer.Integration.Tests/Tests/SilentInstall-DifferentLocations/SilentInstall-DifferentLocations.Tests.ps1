$currentDir = Split-Path -parent $MyInvocation.MyCommand.Path
Set-Location $currentDir

# mapped sync folder for common scripts
. $currentDir\..\common\Utils.ps1
. $currentDir\..\common\CommonTests.ps1

Describe "Silent Install with different install locations" {

    $InstallDir = "C:\temp dir\Elasticsearch\"
    $DataDir = "C:\foo\data"
    $ConfigDir = "C:\bar\config"
    $LogsDir = "C:\baz\logs"

    $InstallLocations = "INSTALLDIR=$InstallDir","DATADIRECTORY=$DataDir","CONFIGDIRECTORY=$ConfigDir","LOGSDIRECTORY=$LogsDir"

    Invoke-SilentInstall -ExeArgs $InstallLocations

    Context-ElasticsearchService

    Context-PingNode -XPackSecurityInstalled $true

    Context-EsHomeEnvironmentVariable -Expected $InstallDir

    Context-EsConfigEnvironmentVariable -Expected $ConfigDir

    Context-PluginsInstalled

    Context-MsiRegistered

    Context-ServiceRunningUnderAccount -Expected "LocalSystem"

    Context-EmptyEventLog
  
    Context-ClusterNameAndNodeName -Expected @{ Credentials = "elastic:changeme" }

    Context-ElasticsearchConfiguration -Expected @{Data = $DataDir; Logs=  $LogsDir }

    Context-JvmOptions


}
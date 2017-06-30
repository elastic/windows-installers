$currentDir = Split-Path -parent $MyInvocation.MyCommand.Path
Set-Location $currentDir

# mapped sync folder for common scripts
. $currentDir\..\common\Utils.ps1
. $currentDir\..\common\CommonTests.ps1

$InstallDir = "C:\temp dir\Elasticsearch\"
$DataDir = "C:\foo\data"
$ConfigDir = "C:\bar\config"
$LogsDir = "C:\baz\logs"

Describe "Silent Install with different install locations" {

    $InstallLocations = "INSTALLDIR=$InstallDir","DATADIRECTORY=$DataDir","CONFIGDIRECTORY=$ConfigDir","LOGSDIRECTORY=$LogsDir"

    Invoke-SilentInstall -ExeArgs $InstallLocations

    Context-ElasticsearchService

    Context-PingNode -XPackSecurityInstalled $false

    Context-EsHomeEnvironmentVariable -Expected $InstallDir

    Context-EsConfigEnvironmentVariable -Expected $ConfigDir

    Context-PluginsInstalled

    Context-MsiRegistered

    Context-ServiceRunningUnderAccount -Expected "LocalSystem"

    Context-EmptyEventLog
  
    Context-ClusterNameAndNodeName

    Context-ElasticsearchConfiguration -Expected @{Data = $DataDir; Logs = $LogsDir }

    Context-JvmOptions
}

Describe "Silent Uninstall with different install locations" {

    Invoke-SilentUninstall

    Context "Ping node" {
        It "Elasticsearch node should not be running" {
            try {
                $Response = Invoke-RestMethod http://localhost:9200
                $Response | Should Be $null
            }
            catch {
                $_.Exception.Message | Should Be "Unable to connect to the remote server"
            }
        }
    }

    Context "ES_CONFIG Environment Variable" {
        $EsConfig = Get-MachineEnvironmentVariable "ES_CONFIG"
        It "ES_CONFIG Environment variable should be null" {
            $EsConfig | Should Be $null
        }
    }

    Context "ES_HOME Environment Variable" {
        $EsConfig = Get-MachineEnvironmentVariable "ES_HOME"
        It "ES_HOME Environment variable should be null" {
            $EsConfig | Should Be $null
        }
    }

    Context "MSI Product" {
        $Product = Get-ElasticsearchWin32Product
        It "MSI should not be registered" {
            $Product | Should Be $null
        }
    }

    Context "Elasticsearch Service" {
        $Service = Get-ElasticsearchWin32Service
        It "Service should not be registered" {
            $Service | Should Be $null
        }
    }

    Context "Installation directory" {
        It "Installation directory should not exist" {
            $InstallDir | Should Not Exist
        }
    }
}
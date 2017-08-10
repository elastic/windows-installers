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
        $EsConfig = Get-MachineEnvironmentVariable "CONF_DIR"
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
        $ProgramFiles = Get-ProgramFilesFolder
        $ExpectedHomeFolder = Join-Path -Path $ProgramFiles -ChildPath "Elastic\Elasticsearch\"
        It "Installation directory should not exist" {
            $ExpectedHomeFolder | Should Not Exist
        }
    }
}
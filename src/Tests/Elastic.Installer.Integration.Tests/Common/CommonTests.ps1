<#
	Contains Pester tests common to test scenarios
#>
$pester = "Pester"
if(-not(Get-Module -Name $pester)) 
{ 
	if(Get-Module -Name $pester -ListAvailable) { 
       	Import-Module -Name $pester 
    }  
	else { 
		PowerShellGet\Install-Module $pester -Force
		Import-Module $pester
	}
}

function Context-ElasticsearchService($Expected) {
    $Expected = Merge-Hashtables @{
            Status="Running"
            StartType="Automatic"
            Name="Elasticsearch"
            DisplayName="Elasticsearch"
        } $Expected

    Context "Elasticsearch service" {
        $Service = Get-ElasticsearchService

        It "Service is not null" {
            $Service | Should Not Be $null
        }

        It "Service status is $($Expected.Status)" {
            $Service.Status | Should Be $($Expected.Status)
        }

        It "Service can be stopped" {
            $Service.CanStop | Should Be $true
        }

        It "Service can be shutdown" {
            $Service.CanStop | Should Be $true
        }

        It "Service cannot be paused and continued" {
            $Service.CanPauseAndContinue | Should Be $false
        }

        It "Service startup type is $($Expected.StartType)" {
            $Service.StartType | Should Be $($Expected.StartType)
        }

        It "Service name is $($Expected.Name)" {
            $Service.Name | Should Be $($Expected.Name)
        }

        It "Service display name is $($Expected.DisplayName)" {
            $Service.DisplayName | Should Be $($Expected.DisplayName)
        }
    }
}

function Context-PingNode($XPackSecurityInstalled) {
    Context "Ping node" {
        $Response = Ping-Node

        It "Ping is successful" {
            $Response.Success | Should Be $true
        }

        $XPackSecurityIsInstalled = "X-Pack Security is installed"
        if (! ($XPackSecurityInstalled)) {
            $XPackSecurityIsInstalled = "X-Pack Security is not installed"
        }

        It $XPackSecurityIsInstalled {
            $Response.XPackSecurityInstalled | Should Be $XPackSecurityInstalled
        }
    }
}

function Context-PluginsInstalled($Expected) {
    $EsHome = Get-MachineEnvironmentVariable "ES_HOME"
    $DefaultPluginBat = Join-Path -Path $EsHome -ChildPath "bin\elasticsearch-plugin.bat"

    $Expected = Merge-Hashtables @{
        EsPluginBat = $DefaultPluginBat
        Plugins = @("ingest-attachment", "ingest-geoip", "x-pack")
    } $Expected


    Context "Plugins installed" {
        $Plugins = & $($Expected.EsPluginBat) "list"

        if (($Expected.Plugins -eq $null) -or $($Expected.Plugins).Count -eq 0) {
            It "No plugins should be installed" {
                $Plugins | Should BeNullOrEmpty
            }
        }
        else {
            foreach($Plugin in $Expected.Plugins) {
                It "Plugin $Plugin is installed" {
                    {$Plugins -contains $Plugin} | Should Be $true
                }
            }
        }
    }
}

function Context-EsHomeEnvironmentVariable($Expected) {
    Context "ES_HOME Environment Variable" {
        $EsHome = Get-MachineEnvironmentVariable "ES_HOME"

        It "ES_HOME Environment Variable is not null" {
            $EsHome | Should Not Be $null
        }

        It "ES_HOME Environment variable set to $Expected" {
            $EsHome | Should Be $Expected
        }
    }
}

function Context-EsConfigEnvironmentVariable($Expected) {
    Context "ES_CONFIG Environment Variable" {
        $EsConfig = Get-MachineEnvironmentVariable "ES_CONFIG"

        It "ES_CONFIG is not null" {
            $EsConfig | Should Not Be $null
        }

        It "ES_CONFIG Environment variable set to $Expected" {
            $EsConfig | Should Be $Expected
        }
    }
}

function Context-MsiRegistered($Expected) {
    $Expected = Merge-Hashtables @{
            Name = "Elasticsearch $env:EsVersion"
            Caption = "Elasticsearch $env:EsVersion"
            Version = $env:EsVersion
        } $Expected


    Context "MSI registered" {
        $Product = Get-ElasticsearchWin32Product

        It "MSI is registered" {
            $Product | Should Not Be $null
        }

        It "MSI should have an Identifying number" {
            $Product.IdentifyingNumber | Should Not Be $null
        }

        It "MSI should have Name that matches the release" {
            $Product.Name | Should Be $($Expected.Name)
        }

        It "MSI should have Caption that matches the release" {
            $Product.Caption | Should Be $($Expected.Caption)
        }

        It "MSI should have Vendor of Elastic" {
            $Product.Vendor | Should Be "Elastic"
        }

        It "MSI should have the correct Version" {
            $Product.Version | Should BeExactly $($Expected.Version)
        }
    }
}

function Context-ServiceRunningUnderAccount($Expected) {
    Context "Service installed to run with account" {
        $Service = Get-ElasticsearchWin32Service

        It "Service is registered" {
            $Service | Should Not Be $null
        }

        It "Service configured to run under account $Expected" {
            $Service.StartName | Should BeExactly $Expected
        }

        $EsHome = Get-MachineEnvironmentVariable "ES_HOME"
        $EsExe = Join-Path -Path $EsHome -ChildPath "bin\elasticsearch.exe"

        It "Service PathName points to $EsExe" {
            $($Service.PathName.Trim('"')) | Should Be $EsExe
        }
    }
}

function Context-EmptyEventLog() {
    Context "Event log" {
        $ElasticsearchEventLogs = Get-EventLog -LogName Application -Source Elastic*

        It "Event log is empty" {
            $ElasticsearchEventLogs | Should Be $null
        }
    }
}

function Context-ClusterNameAndNodeName($Expected) {
    $Expected = Merge-Hashtables @{
            ClusterName = "elasticsearch"
            NodeName = $($env:COMPUTERNAME)
            Credentials = ""
			Host = "localhost"
			Port = "9200"
        } $Expected

    Context "Cluster name and Node name" {
        if ($Expected.Credentials) {
            $Bytes = [System.Text.Encoding]::UTF8.GetBytes($Expected.Credentials)
            $AuthHeaderValue = [Convert]::ToBase64String($Bytes)
            $Response = Invoke-RestMethod "http://$($Expected.Host):$($Expected.Port)" -Headers @{Authorization=("Basic {0}" -f $AuthHeaderValue)}
        }
        else {
            $Response = Invoke-RestMethod "http://$($Expected.Host):$($Expected.Port)"
        }

        It "cluster_name should be $($Expected.ClusterName)" {
            $Response.cluster_name | Should Be $($Expected.ClusterName)
        }

        It "name should be $($Expected.NodeName)" {
            $Response.name | Should Be $($Expected.NodeName)
        }
    }
}

function Context-ElasticsearchConfiguration ([HashTable]$Expected) {

    $EsConfig = Get-MachineEnvironmentVariable "ES_CONFIG"
    $EsData = Split-Path $EsConfig -Parent | Join-Path -ChildPath "data"
    $EsLogs = Split-Path $EsConfig -Parent | Join-Path -ChildPath "logs"

    $Expected = Merge-Hashtables @{
            BootstrapMemoryLock = $true
            NodeData = $true
            NodeIngest = $true
            NodeMaster = $true
            NodeMaxLocalStorageNodes = 1
            Data = $EsData
            Logs = $EsLogs
    } $Expected

    Context "Elasticsearch Yaml Configuration" {

        $ConfigLines = "$EsConfig\elasticsearch.yml"

        It "elasticsearch.yml exists in $EsConfig" {
            $ConfigLines | Should Exist
        }

        It "bootstrap.memory_lock set to $($Expected.BootstrapMemoryLock)" {
            $ConfigLines | Should Contain "bootstrap.memory_lock: $($Expected.BootstrapMemoryLock)"
        }

        It "node.data set to to $($Expected.NodeData)" {
            $ConfigLines | Should Contain "node.data: $($Expected.NodeData)"
        }

        It "node.ingest set to $($Expected.NodeIngest)" {
            $ConfigLines | Should Contain "node.ingest: $($Expected.NodeIngest)"
        }

        It "node.master set to $($Expected.NodeMaster)" {
            $ConfigLines | Should Contain "node.master: $($Expected.NodeMaster)"
        }

        It "node.max_local_storage_nodes set to $($Expected.NodeMaxLocalStorageNodes)" {
            $ConfigLines | Should Contain "node.max_local_storage_nodes: $($Expected.NodeMaxLocalStorageNodes)"
        }

        It "path.data set to $($Expected.Data)" {
            $ConfigLines | Should Contain ([regex]::Escape("path.data: $($Expected.Data)"))
        }

        It "path.logs set to $($Expected.Logs)" {
            $ConfigLines | Should Contain ([regex]::Escape("path.logs: $($Expected.Logs)"))
        }
    }
}

function Context-JvmOptions ($Expected) {
    if (!($Expected)) {
        $Expected = (Get-TotalPhysicalMemory) / 2
    }

    Context "JVM Configuration" {
        $EsConfig = Get-MachineEnvironmentVariable "ES_CONFIG"
        $ConfigLines = "$EsConfig\jvm.options"

        It "jvm.options should exist in $EsConfig" {
            $ConfigLines | Should Exist
        }

        It "Min Heap size should be set to $($Expected)m" {
            $ConfigLines | Should Contain ([regex]::Escape("-Xmx$($Expected)m"))
        }

        It "Max Heap size should be set to $($Expected)m" {
            $ConfigLines | Should Contain ([regex]::Escape("-Xms$($Expected)m"))
        }
    }
}

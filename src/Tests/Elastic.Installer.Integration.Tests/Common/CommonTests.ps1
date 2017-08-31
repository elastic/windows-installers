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
        Plugins = @()
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
    Context "CONF_DIR Environment Variable" {
        $EsConfig = Get-MachineEnvironmentVariable "CONF_DIR"

        It "CONF_DIR is not null" {
            $EsConfig | Should Not Be $null
        }

        It "CONF_DIR Environment variable set to $Expected" {
            $EsConfig | Should Be $Expected
        }
    }
}

function Context-MsiRegistered($Expected) {
    $Expected = Merge-Hashtables @{
            Name = "Elasticsearch $env:EsVersion"
            Caption = "Elasticsearch $env:EsVersion"
            # version is always without any prerelease suffix
            Version = $($env:EsVersion).Split("-")[0]
        } $Expected


    Context "MSI registered" {
        $Product = Get-ElasticsearchWin32Product

        It "MSI is registered" {
            $Product | Should Not Be $null
        }

        It "MSI should have an Identifying number" {
            $Product.IdentifyingNumber | Should Not Be $null
        }

        It "MSI should have Name that matches $($Expected.Name)" {
            $Product.Name | Should Be $($Expected.Name)
        }

        It "MSI should have Caption that matches $($Expected.Caption)" {
            $Product.Caption | Should Be $($Expected.Caption)
        }

        It "MSI should have Vendor of Elastic" {
            $Product.Vendor | Should Be "Elastic"
        }

        It "MSI should have the version $($Expected.Version)" {
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

function Context-EventContainsFailedInstallMessage($StartDate, $Version) {
    Context "Event log" {
		$failedMessage = "Product: Elasticsearch $Version -- Installation failed."

        It "Event log contains '$failedMessage'" {
			$failedLog = Get-EventLog -LogName Application -Source MsiInstaller -After $StartDate | `
				Where { $_.message -Match $failedMessage }

            $failedLog | Should Not Be $null
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

    $EsConfig = Get-MachineEnvironmentVariable "CONF_DIR"
    $EsData = Split-Path $EsConfig -Parent | Join-Path -ChildPath "data"
    $EsLogs = Split-Path $EsConfig -Parent | Join-Path -ChildPath "logs"

    $Expected = Merge-Hashtables @{
            BootstrapMemoryLock = $false
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
			$ConfigLines | Should Contain "bootstrap.memory_lock: $($Expected.BootstrapMemoryLock.ToString().ToLowerInvariant())"
		}         
           
		It "node.data set to to $($Expected.NodeData)" {
			$ConfigLines | Should Contain "node.data: $($Expected.NodeData.ToString().ToLowerInvariant())"
		}    

		It "node.ingest set to $($Expected.NodeIngest)" {
			$ConfigLines | Should Contain "node.ingest: $($Expected.NodeIngest.ToString().ToLowerInvariant())"
		}

		It "node.master set to $($Expected.NodeMaster)" {
			$ConfigLines | Should Contain "node.master: $($Expected.NodeMaster.ToString().ToLowerInvariant())"
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
    	$totalPhysicalMemory = Get-TotalPhysicalMemory
    	
    	if ($totalPhysicalMemory -le 4096) {
    		$Expected = $totalPhysicalMemory / 2
    	}
    	else {
    		$Expected = 2048
    	}
    }

    Context "JVM Configuration" {
        $EsConfig = Get-MachineEnvironmentVariable "CONF_DIR"
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

function Context-InsertData($Domain, $Port, $Credentials) {
	if (!$Domain) {
		$Domain = "localhost"
	}

	if (!$Port) {
		$Port = "9200"
	}

	Context "Insert Data" {
		try {
			$requests = @(
				'{ "index" : { "_index" : "test", "_type" : "type1", "_id" : "1" } }'
				'{ "field1" : "value1" }'
				'{ "delete" : { "_index" : "test", "_type" : "type1", "_id" : "2" } }'
				'{ "create" : { "_index" : "test", "_type" : "type1", "_id" : "3" } }'
				'{ "field1" : "value3" }'
				'{ "update" : {"_id" : "1", "_type" : "type1", "_index" : "test"} }'
				'{ "doc" : {"field2" : "value2"} }'
			)

			$body = [string]::Join("`n", $requests) + "`n"

			if ($Credentials) {
				$bytes = [Text.Encoding]::UTF8.GetBytes(($Credentials))
				$authHeaderValue = [Convert]::ToBase64String($bytes)
				$response = Invoke-RestMethod "http://$($Domain):$Port/_bulk" -Method Post `
								-Headers @{Authorization=("Basic {0}" -f $authHeaderValue)} `
								-Body $body
			}
			else {
				$response = Invoke-RestMethod "http://$($Domain):$Port/_bulk" -Method Post -Body $body
			}

			It "Should have no errors" {
				$response.errors | Should Be $false
			}

			It "Takes longer than 0ms" {
				$response.took | Should BeGreaterThan 0
			}
		}
		catch {
			$Code = $_.Exception.Response.StatusCode.value__
			$Description = $_.Exception.Response.StatusDescription

			if ($_) {
				$response = $_ | ConvertFrom-Json
				if ($response) {
					log "bulk call failed: $response" -l Error
				}
			}
			else {
				log "bulk call failed. code: $Code, description: $Description" -l Error
			}
		}
	}
}

function Context-ReadData($Domain, $Port, $Credentials) {
	if (!$Domain) {
		$Domain = "localhost"
	}

	if (!$Port) {
		$Port = "9200"
	}

	Context "Read Data" {
		try {
			if ($Credentials) {
				$bytes = [Text.Encoding]::UTF8.GetBytes(($Credentials))
				$authHeaderValue = [Convert]::ToBase64String($bytes)
				$response = Invoke-RestMethod "http://$($Domain):$Port/test/type1/_search" -Method Get `
								-Headers @{Authorization=("Basic {0}" -f $authHeaderValue)} `
			}
			else {
				$response = Invoke-RestMethod "http://$($Domain):$Port/test/type1/_search" -Method Get
			}

			It "Have expected count" {
				$response.hits.total | Should Be 2
			}

			it "should not time out" {
				$response.timed_out | Should Be $false
			}

			It "Takes longer than 0ms" {
				$response.took | Should BeGreaterThan 0
			}
		}
		catch {
			$Code = $_.Exception.Response.StatusCode.value__
			$Description = $_.Exception.Response.StatusDescription

			if ($_) {
				$response = $_ | ConvertFrom-Json
				if ($response) {
					log "bulk call failed: $response" -l Error
				}
			}
			else {
				log "bulk call failed. code: $Code, description: $Description" -l Error
			}
		}
	}
}

function Context-EmptyInstallDirectory($Path) {    
	Context "Installation directory" {
        It "Installation directory should not exist or is empty" {
			if (Test-Path $Path) {
				$files = Get-ChildItem $Path
				$files | Should Be $null
			}
			else {
				$Path | Should Not Exist
			}            
        }
    }
}

function Context-EnvironmentVariableNull($Name) {
	Context "$Name Environment Variable" {
        $envVar = Get-MachineEnvironmentVariable $Name
        It "$Name Environment variable should be null" {
            $envVar | Should Be $null
        }
    }
}

function Context-MsiNotRegistered() {
	Context "MSI Product" {
        $Product = Get-ElasticsearchWin32Product
        It "MSI should not be registered" {
            $Product | Should Be $null
        }
    }
}

function Context-ElasticsearchServiceNotInstalled() {
	Context "Elasticsearch Service" {
        $Service = Get-ElasticsearchWin32Service
        It "Service should not be registered" {
            $Service | Should Be $null
        }
    }
}

function Context-NodeNotRunning($Domain, $Port, $Credentials) {
	if (!$Domain) {
		$Domain = "localhost"
	}

	if (!$Port) {
		$Port = "9200"
	}

	Context "Ping node" {
        It "Elasticsearch node should not be running" {
            try {
				if ($Credentials) {
					$bytes = [Text.Encoding]::UTF8.GetBytes(($Credentials))
					$authHeaderValue = [Convert]::ToBase64String($bytes)
					$response = Invoke-RestMethod "http://$($Domain):$Port" `
								-Headers @{Authorization=("Basic {0}" -f $authHeaderValue)} `
				}
				else {
					$response = Invoke-RestMethod "http://$($Domain):$Port"
				}

                $Response | Should Be $null
            }
            catch {
                $_.Exception.Message | Should Be "Unable to connect to the remote server"
            }
        }
    }
}


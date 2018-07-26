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
			StartIfNotRunning = $true
			CanStop = $true
			CanShutdown = $true
		} $Expected

    Context "Elasticsearch service" {
        $Service = Get-ElasticsearchService

        It "Service is not null" {
            $Service | Should Not Be $null
        }

		if ($Service.Status -eq "StartPending" -and $Expected.Status -eq "Running") {
			$timeSpan = New-TimeSpan -Seconds 10
			Write-Output "Service is currently $($Service.Status). Wait $timeSpan for running"
			try {
				$Service.WaitForStatus("Running", $timeSpan)
			}
			catch {
				# swallow exception as following assertion will test status.
			}
		}


        It "Service status is $($Expected.Status)" {
            $Service.Status | Should Be $Expected.Status

			# BUG: Upgrading from Elasticsearch 5.5.0-2, to 5.5.3+, the service is not started after upgrading.
			# Start the service if commanded to do so
			if ($Expected.StartIfNotRunning -and ($Expected.Status -eq "StopPending" -or $Expected.Status -eq "Stopped")) {
				Write-Output "Service is currently $($Service.Status). Attempting to start"
				$timeSpan = New-TimeSpan -Seconds 10
				if ($Service.Status -eq "StopPending") {
					$Service.WaitForStatus("Stopped", $timeSpan)
				}

				$Service | Start-Service
			}
        }

        It "Service can be stopped $($Expected.CanStop)" {
            $Service.CanStop | Should Be $($Expected.CanStop)
        }

        It "Service can be shutdown $($Expected.CanShutdown)" {
            $Service.CanShutdown | Should Be $($Expected.CanShutdown)
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

function Context-PingNode([switch]$XPackSecurityInstalled) {
    Context "Ping node" {
        $Response = Ping-Node

        It "Ping is successful" {
            $Response.Success | Should Be $true
        }

        $XPackSecurityIsInstalled = "X-Pack Security is installed/enabled"
        if (! ($XPackSecurityInstalled)) {
            $XPackSecurityIsInstalled = "X-Pack Security is not installed/enabled"
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
		log "Installed plugins: $Plugins" -Level Debug

        if (($Expected.Plugins -eq $null) -or $($Expected.Plugins).Count -eq 0) {
            It "No plugins should be installed" {
                $Plugins | Should BeNullOrEmpty
            }
        }
        else {
            foreach($Plugin in $Expected.Plugins) {
                It "Plugin $Plugin is installed" {
                    $Plugins -contains $Plugin | Should Be $true
                }
            }
        }
    }
}

function Context-EsHomeEnvironmentVariable($Expected = (Join-Path -Path $(Get-ProgramFilesFolder) -ChildPath $(Get-ChildPath))) {
    Context "ES_HOME Environment Variable" {
        $EsHome = Get-MachineEnvironmentVariable "ES_HOME"

        It "ES_HOME Environment Variable is not null" {
            $EsHome | Should Not Be $null
        }

		# trim trailing backslashes for comparison.
        It "ES_HOME Environment variable set to $Expected" {
            $EsHome.TrimEnd('\') | Should Be $Expected.TrimEnd('\')
        }
    }
}

function Context-EsConfigEnvironmentVariable($Expected) {
	$Expected = Merge-Hashtables @{
        Version = $Global:Version
        Path = Join-Path -Path $($env:ALLUSERSPROFILE) -ChildPath "Elastic\Elasticsearch\config"
    } $Expected

	$esConfigEnvVar = Get-ConfigEnvironmentVariableForVersion -Version $Expected.Version

    Context "$esConfigEnvVar Environment Variable" {
        $EsConfig = Get-MachineEnvironmentVariable $esConfigEnvVar

        It "$esConfigEnvVar is not null" {
            $EsConfig | Should Not Be $null
        }

        It "$esConfigEnvVar Environment variable set to $($Expected.Path)" {
            $EsConfig | Should Be $Expected.Path
        }
    }
}

function Context-MsiRegistered($Expected) {
	$version = $Global:Version

    $Expected = Merge-Hashtables @{
            Name = "Elasticsearch $($version.FullVersion)"
            Caption = "Elasticsearch $($version.FullVersion)"
            # Product Version is always without any prerelease suffix
            Version = "$($version.Major).$($version.Minor).$($version.Patch)"
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

function Context-ServiceRunningUnderAccount($Expected = "LocalSystem") {
    Context "Service installed to run with account" {
        $Service = Get-ElasticsearchWin32Service

        It "Service is registered" {
            $Service | Should Not Be $null
        }

        It "Service configured to run under account $Expected" {
            $Service.StartName | Should Be $Expected
        }

        $EsHome = Get-MachineEnvironmentVariable "ES_HOME"
        $EsExe = Join-Path -Path $EsHome -ChildPath "bin\elasticsearch.exe"

        It "Service PathName points to $EsExe" {
            $($Service.PathName.Trim('"')) | Should Be $EsExe
        }
    }
}

function Context-EmptyEventLog($StartDate = $Global:InstallStartDate, $Version = $Global:Version) {

    Context "Event log" {
        $ElasticsearchEventLogs = Get-EventLog -LogName Application -Source Elastic*

		if ((Compare-SemanticVersion $Version $(ConvertTo-SemanticVersion "6.0.0") -le 0) `
			-and $Version.SourceType -ne "Compile") {

			$failedMessage = "ElasticsearchCleanupAction.cs"
			# event log may contain events similar to:
			#
			# System.ComponentModel.Win32Exception (0x80004005): The system cannot find the file specified
			# 
			# when running Cleanup action in the old installer uninstall process, 
			# because the old install plugin script no longer exists. Filter these out
			$ElasticsearchEventLogs = Get-EventLog -LogName Application -Source Elastic* -After $StartDate `
				| Where { $_.Message -notmatch $failedMessage } | Format-List | Out-String

			It "Event log doesn't contain unexpected messages" {
				$ElasticsearchEventLogs | Should BeNullOrEmpty
			}
		}
		else {
			# convert to string so in the event of error we can see what the log entries actually are in the test output
			$ElasticsearchEventLogs = Get-EventLog -LogName Application -Source Elastic* -After $StartDate `
				| Format-List | Out-String

			It "Event log is empty" {
				$ElasticsearchEventLogs | Should BeNullOrEmpty
			}
		}
	}
}

function Context-EventContainsFailedInstallMessage($StartDate = $Global:InstallStartDate, $Version) {
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
            $Response = Invoke-RestMethod "http://$($Expected.Host):$($Expected.Port)" `
						-Headers @{Authorization=("Basic {0}" -f $AuthHeaderValue)} -ContentType "application/json"
        }
        else {
            $Response = Invoke-RestMethod "http://$($Expected.Host):$($Expected.Port)" -ContentType "application/json"
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
	$Expected = Merge-Hashtables @{
        BootstrapMemoryLock = $false
        NodeData = $true
        NodeIngest = $true
        NodeMaster = $true
        NodeMaxLocalStorageNodes = 1
		Version = $Global:Version
    } $Expected

    $esConfigEnvVar = Get-ConfigEnvironmentVariableForVersion -Version $Expected.Version
	$EsConfig = Get-MachineEnvironmentVariable $esConfigEnvVar
    $EsData = Split-Path $EsConfig -Parent | Join-Path -ChildPath "data"
    $EsLogs = Split-Path $EsConfig -Parent | Join-Path -ChildPath "logs"

	$Expected = Merge-Hashtables @{
		Data = $EsData
        Logs = $EsLogs
	} $Expected

    Context "Elasticsearch Yaml Configuration" {

        $ConfigLines = "$EsConfig\elasticsearch.yml"

        It "elasticsearch.yml exists in $EsConfig" {
            $ConfigLines | Should Exist
        }

		It "bootstrap.memory_lock set to $($Expected.BootstrapMemoryLock)" {
			$ConfigLines | Should FileContentMatchExactly "bootstrap.memory_lock: $($Expected.BootstrapMemoryLock.ToString().ToLowerInvariant())"
		}         
           
		It "node.data set to to $($Expected.NodeData)" {
			$ConfigLines | Should FileContentMatchExactly "node.data: $($Expected.NodeData.ToString().ToLowerInvariant())"
		}    

		It "node.ingest set to $($Expected.NodeIngest)" {
			$ConfigLines | Should FileContentMatchExactly "node.ingest: $($Expected.NodeIngest.ToString().ToLowerInvariant())"
		}

		It "node.master set to $($Expected.NodeMaster)" {
			$ConfigLines | Should FileContentMatchExactly "node.master: $($Expected.NodeMaster.ToString().ToLowerInvariant())"
		}

        It "node.max_local_storage_nodes set to $($Expected.NodeMaxLocalStorageNodes)" {
            $ConfigLines | Should FileContentMatchExactly "node.max_local_storage_nodes: $($Expected.NodeMaxLocalStorageNodes)"
        }

        It "path.data set to $($Expected.Data)" {
            $ConfigLines | Should FileContentMatchExactly ([regex]::Escape("path.data: $($Expected.Data)"))
        }

        It "path.logs set to $($Expected.Logs)" {
            $ConfigLines | Should FileContentMatchExactly ([regex]::Escape("path.logs: $($Expected.Logs)"))
        }
    }
}

function Context-JvmOptions ($Expected) {
	$totalPhysicalMemory = Get-TotalPhysicalMemory  	
    if ($totalPhysicalMemory -le 4096) {
    	$defaultMemory = $totalPhysicalMemory / 2
    }
    else {
    	$defaultMemory = 2048
    }
	
	$Expected = Merge-Hashtables @{
		Version = $Global:Version
		Memory = $defaultMemory
    } $Expected

    Context "JVM Configuration" {
        $esConfigEnvVar = Get-ConfigEnvironmentVariableForVersion -Version $Expected.Version
		$EsConfig = Get-MachineEnvironmentVariable $esConfigEnvVar
        $ConfigLines = "$EsConfig\jvm.options"

        It "jvm.options should exist in $EsConfig" {
            $ConfigLines | Should Exist
        }

        It "Min Heap size should be set to $($Expected.Memory)m" {
            $ConfigLines | Should FileContentMatchExactly ([regex]::Escape("-Xmx$($Expected.Memory)m"))
        }

        It "Max Heap size should be set to $($Expected.Memory)m" {
            $ConfigLines | Should FileContentMatchExactly ([regex]::Escape("-Xms$($Expected.Memory)m"))
        }
    }
}

function Context-InsertData($Domain = "localhost", $Port = 9200, $Credentials) {

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
			$url = "http://$($Domain):$Port/_bulk?refresh"

			if ($Credentials) {
				$bytes = [Text.Encoding]::UTF8.GetBytes(($Credentials))
				$authHeaderValue = [Convert]::ToBase64String($bytes)
				$response = Invoke-RestMethod $url -Method Post `
								-Headers @{Authorization=("Basic {0}" -f $authHeaderValue)} `
								-Body $body -ContentType "application/json"
			}
			else {
				$response = Invoke-RestMethod $url -Method Post -Body $body -ContentType "application/json"
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

function Context-ReadData($Domain = "localhost", $Port = 9200, $Credentials) {

	Context "Read Data" {
		try {
			if ($Credentials) {
				$bytes = [Text.Encoding]::UTF8.GetBytes(($Credentials))
				$authHeaderValue = [Convert]::ToBase64String($bytes)
				$response = Invoke-RestMethod "http://$($Domain):$Port/test/type1/_search" -Method Get `
								-Headers @{Authorization=("Basic {0}" -f $authHeaderValue)} -ContentType "application/json"
			}
			else {
				$response = Invoke-RestMethod "http://$($Domain):$Port/test/type1/_search" `
				                -Method Get -ContentType "application/json"
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

function Context-EmptyInstallDirectory($Path = (Join-Path -Path $(Get-ProgramFilesFolder) -ChildPath $(Get-ChildPath))) {    
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

function Context-EsConfigEnvironmentVariableNull($Version = $Global:Version) {
	$Name = Get-ConfigEnvironmentVariableForVersion -Version $Version

	Context "$Name Environment Variable" {
        $envVar = Get-MachineEnvironmentVariable $Name
        It "$Name Environment variable should be null" {
            $envVar | Should Be $null
        }
    }
}

function Context-EsHomeEnvironmentVariableNull() {
	$EsHome = Get-MachineEnvironmentVariable "ES_HOME"

	Context "ES_HOME Environment Variable" {
        It "ES_HOME Environment variable should be null" {
            $EsHome | Should Be $null
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

function Context-NodeNotRunning($Domain = "localhost", $Port = 9200, $Credentials) {
	Context "Ping node" {
        It "Elasticsearch node should not be running" {
            try {
				if ($Credentials) {
					$bytes = [Text.Encoding]::UTF8.GetBytes(($Credentials))
					$authHeaderValue = [Convert]::ToBase64String($bytes)
					$response = Invoke-RestMethod "http://$($Domain):$Port" `
								-Headers @{Authorization=("Basic {0}" -f $authHeaderValue)} -ContentType "application/json"
				}
				else {
					$response = Invoke-RestMethod "http://$($Domain):$Port" -ContentType "application/json"
				}

                $Response | Should Be $null
            }
            catch {
                $_.Exception.Message | Should Be "Unable to connect to the remote server"
            }
        }
    }
}

function Context-FiddlerSessionContainsEntry() {
	Context "Fiddler session" {
		$session = Get-FiddlerSession
		$artifactsUrl = "artifacts.elastic.co"

		log $session

		It "Contains $artifactsUrl" {
			$session | Should Match ([regex]::Escape($artifactsUrl))
		}
	}
}

function Context-DirectoryExists([string]$Path, [switch]$DeleteAfter) {
	Context "Directory $Path" {
		It "Directory exists" {
			Test-Path $Path | Should Be $true
		}
	}

	if ((Test-Path $Path) -and $DeleteAfter) {
		Remove-Item $Path -Force -Recurse
	}
}

function Context-DirectoryNotExist([string]$Path, [switch]$DeleteAfter) {
	Context "Directory $Path" {
		It "Directory does not exist" {
			Test-Path $Path | Should Be $false
		}
	}

	if ((Test-Path $Path) -and $DeleteAfter) {
		Remove-Item $Path -Force -Recurse
	}
}

function Context-DataDirectories($Version=$Global:Version, [string[]]$Path, [switch]$DeleteAfter)
{
	$620Release = ConvertTo-SemanticVersion "6.2.0"

    # Expect the directories to exist for any official release from 6.2.0+, or compiled from source
	if ((Compare-SemanticVersion $Version $620Release) -lt 0 -and $Version.SourceType -ne "Compile") {
		foreach($p in $Path) {
			Context-DirectoryNotExist -Path $p -DeleteAfter:$DeleteAfter
		}
	}
	else {
		foreach($p in $Path) {
			Context-DirectoryExists -Path $p -DeleteAfter:$DeleteAfter
		}
	}
}

function Context-RegistryEmpty() {
	Context "Registry keys" {
		It "Registry keys do not exist under HKLM:\SOFTWARE\Elastic\Elasticsearch" {
			Test-Path 'HKLM:\SOFTWARE\Elastic\Elasticsearch' | Should Be $false
		}
	}
}

function Context-RegistryForVersion($Version=$Global:Version) {
	Context "Registry keys" {
		It "Registry keys exist under HKLM:\SOFTWARE\Elastic\Elasticsearch\$($Version.FullVersion)" {
			Test-Path "HKLM:\SOFTWARE\Elastic\Elasticsearch\$($Version.FullVersion)" | Should Be $true
		}
	}
}


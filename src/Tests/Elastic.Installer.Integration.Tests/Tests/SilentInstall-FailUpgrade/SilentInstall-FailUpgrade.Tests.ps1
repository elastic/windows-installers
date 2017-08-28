$currentDir = Split-Path -parent $MyInvocation.MyCommand.Path
Set-Location $currentDir

# mapped sync folder for common scripts
. $currentDir\..\common\Utils.ps1
. $currentDir\..\common\CommonTests.ps1

$credentials = "elastic:changeme"

Describe -Tag 'PreviousVersion' "Silent Install fail upgrade - Install previous version" {

	$previousVersion = $env:PreviousEsVersion
	
    Invoke-SilentInstall -Version $previousVersion

    Context-ElasticsearchService

    Context-PingNode -XPackSecurityInstalled $false

    $ProgramFiles = Get-ProgramFilesFolder
    $ExpectedHomeFolder = Join-Path -Path $ProgramFiles -ChildPath "Elastic\Elasticsearch\"

    Context-EsHomeEnvironmentVariable -Expected $ExpectedHomeFolder

    $ProfileFolder = $env:ALLUSERSPROFILE
    $ExpectedConfigFolder = Join-Path -Path $ProfileFolder -ChildPath "Elastic\Elasticsearch\config"

    Context-EsConfigEnvironmentVariable -Expected $ExpectedConfigFolder

	Context-PluginsInstalled

    Context-MsiRegistered -Expected @{
		Name = "Elasticsearch $previousVersion"
		Caption = "Elasticsearch $previousVersion"
		Version = $previousVersion
	}

    Context-ServiceRunningUnderAccount -Expected "LocalSystem"

    Context-EmptyEventLog

	Context-ClusterNameAndNodeName

    Context-ElasticsearchConfiguration

    Context-JvmOptions

	Context-InsertData
}

Describe -Tag 'PreviousVersion' "Silent Install fail upgrade -Fail when Upgrading" {

	$version = $env:EsVersion
	$startDate = Get-Date

	Context "Failed installation" {
		$exitCode = Invoke-SilentInstall -Exeargs @("WIXFAILWHENDEFERRED=1") -Version $version

		It "Exit code is 1603" {
			$exitCode | Should Be 1603
		}
	}

	Context-EventContainsFailedInstallMessage -StartDate $startDate -Version $version

	# Existing version should still be installed and running
    Context-ElasticsearchService

    Context-PingNode -XPackSecurityInstalled $false

    $ProgramFiles = Get-ProgramFilesFolder
    $ExpectedHomeFolder = Join-Path -Path $ProgramFiles -ChildPath "Elastic\Elasticsearch\"

    Context-EsHomeEnvironmentVariable -Expected $ExpectedHomeFolder

    $ProfileFolder = $env:ALLUSERSPROFILE
    $ExpectedConfigFolder = Join-Path -Path $ProfileFolder -ChildPath "Elastic\Elasticsearch\config"

    Context-EsConfigEnvironmentVariable -Expected $ExpectedConfigFolder

    Context-PluginsInstalled

	# previous version still installed
    Context-MsiRegistered -Expected @{
		Name = "Elasticsearch $previousVersion"
		Caption = "Elasticsearch $previousVersion"
		Version = $previousVersion
	}

    Context-ServiceRunningUnderAccount -Expected "LocalSystem"

	Context-ClusterNameAndNodeName

    Context-ElasticsearchConfiguration

    Context-JvmOptions

	# Check inserted data still exists
	Context-ReadData
}

Describe -Tag 'PreviousVersion' "Silent Uninstall fail upgrade - Uninstall old version" {

	$version = $env:PreviousEsVersion

    Invoke-SilentUninstall -Version $version

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

    Context "CONF_DIR Environment Variable" {
        $EsConfig = Get-MachineEnvironmentVariable "CONF_DIR"
        It "CONF_DIR Environment variable should be null" {
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

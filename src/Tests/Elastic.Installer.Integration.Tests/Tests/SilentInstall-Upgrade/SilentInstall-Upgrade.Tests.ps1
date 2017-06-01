$currentDir = Split-Path -parent $MyInvocation.MyCommand.Path
Set-Location $currentDir

# mapped sync folder for common scripts
. $currentDir\..\common\Utils.ps1
. $currentDir\..\common\CommonTests.ps1

Describe -Tag 'PreviousVersion' "Silent Install upgrade - Install previous version" {

	$previousVersion = $env:PreviousEsVersion
	$credentials = "elastic:changeme"

    Invoke-SilentInstall -Version $previousVersion

    Context-ElasticsearchService

    Context-PingNode -XPackSecurityInstalled $true

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

	Context-ClusterNameAndNodeName -Expected @{ Credentials = $credentials }

    Context-ElasticsearchConfiguration

    Context-JvmOptions

	# Insert some data
	Context-InsertData -Credentials "elastic:changeme"
}

Describe -Tag 'PreviousVersion' "Silent Install upgrade -Upgrade to new version" {

	$version = $env:EsVersion
	$credentials = "elastic:changeme"

    Invoke-SilentInstall -Version $version

    Context-ElasticsearchService

    Context-PingNode -XPackSecurityInstalled $true

    $ProgramFiles = Get-ProgramFilesFolder
    $ExpectedHomeFolder = Join-Path -Path $ProgramFiles -ChildPath "Elastic\Elasticsearch\"

    Context-EsHomeEnvironmentVariable -Expected $ExpectedHomeFolder

    $ProfileFolder = $env:ALLUSERSPROFILE
    $ExpectedConfigFolder = Join-Path -Path $ProfileFolder -ChildPath "Elastic\Elasticsearch\config"

    Context-EsConfigEnvironmentVariable -Expected $ExpectedConfigFolder

    Context-PluginsInstalled

    Context-MsiRegistered

    Context-ServiceRunningUnderAccount -Expected "LocalSystem"

    Context-EmptyEventLog

	Context-ClusterNameAndNodeName -Expected @{ Credentials = "elastic:changeme" }

    Context-ElasticsearchConfiguration

    Context-JvmOptions

	# Check inserted data still exists
	Context-ReadData -Credentials $credentials
}

Describe -Tag 'PreviousVersion' "Silent Uninstall upgrade - Uninstall new version" {

	$version = $env:EsVersion

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
        $ProgramFiles = Get-ProgramFilesFolder
        $ExpectedHomeFolder = Join-Path -Path $ProgramFiles -ChildPath "Elastic\Elasticsearch\"
        It "Installation directory should not exist" {
            $ExpectedHomeFolder | Should Not Exist
        }
    }
}
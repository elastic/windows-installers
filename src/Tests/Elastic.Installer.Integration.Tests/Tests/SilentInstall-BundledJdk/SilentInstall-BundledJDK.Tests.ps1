$currentDir = Split-Path -parent $MyInvocation.MyCommand.Path
Set-Location $currentDir

# mapped sync folder for common scripts
. $currentDir\..\common\Utils.ps1
. $currentDir\..\common\CommonTests.ps1
. $currentDir\..\common\Artifact.ps1

Get-Version
Get-PreviousVersions

$Version = $Global:Version
$700Release = ConvertTo-Artifact "7.0.0"
$730Release = ConvertTo-Artifact "7.3.0"

# Bundled JDK for 7.0.0+ only and official MSIs from 7.3.0+
if ((Compare-Artifact $Version $730Release) -gt 0 -or ($Version.Distribution -eq "Zip" -and (Compare-Artifact $Version $700Release) -ge 0)) {

	Describe "Silent Install with bundled JDK $(($Global:Version).Description)" {
	
		Invoke-SilentInstall @(,"INSTALLASSERVICE=false")

		$esHome = Get-MachineEnvironmentVariable "ES_HOME"
		$javaHome = Get-MachineEnvironmentVariable "JAVA_HOME"
		$environmentRegKey = 'Registry::HKEY_LOCAL_MACHINE\System\CurrentControlSet\Control\Session Manager\Environment'

		# Set JAVA_HOME to null so that the Windows Service created next does not pick it up. This has to be set in the registry
		# as any changes to the current environment will not be reflected in the created Windows Service
		Set-ItemProperty -Path $environmentRegKey -Name JAVA_HOME –Value ""

		# manually register the exe as a Windows Service
		& sc.exe create Elasticsearch type= own start= auto binpath= "$($esHome)bin\elasticsearch.exe" displayname= Elasticsearch

		Start-Service Elasticsearch
	
		Ping-Node -Timeout (New-Timespan -Seconds 10)

		# Verify that bundled JDK is being used. Use the process id rather than relying on bundled_jdk = true setting
		Context "Bundled JDK" {
			$response = Invoke-WebRequest "http://localhost:9200/_nodes/jvm?filter_path=nodes.*.jvm.pid" -ContentType "application/json" -UseBasicParsing	
			$javaPath = "Could not extract pid from response"

			if ($response.Content -match '"pid":(\d+)') {
				$processId = $Matches[1]
				$javaPath = Get-Process -Id $processId | select -ExpandProperty Path
			}

			$bundledJavaPath = "$($esHome)jdk\bin\java.exe"

			It "java.exe path is at $bundledJavaPath" {
				$javaPath | Should Be $bundledJavaPath
			}
		}

		# Set JAVA_HOME back
		Set-ItemProperty -Path $environmentRegKey -Name JAVA_HOME –Value $javaHome

		Copy-ElasticsearchLogToOut
	}

	Describe "Silent Uninstall with bundled JDK $(($Global:Version).Description)" {

		$configDirectory = Get-ConfigEnvironmentVariableForVersion | Get-MachineEnvironmentVariable
		$dataDirectory = $configDirectory | Split-Path | Join-Path -ChildPath "data"
		$logsDirectory = $configDirectory | Split-Path | Join-Path -ChildPath "logs"

		Invoke-SilentUninstall

		Context-NodeNotRunning

		Context-EsConfigEnvironmentVariableNull

		Context-EsHomeEnvironmentVariableNull

		Context-MsiNotRegistered

		Context-RegistryEmpty

		Context-EmptyInstallDirectory

		Context-DataDirectories -Path @($configDirectory, $dataDirectory, $logsDirectory) -DeleteAfter
	}

}
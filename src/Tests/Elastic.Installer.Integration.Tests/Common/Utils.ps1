. "$(Split-Path -parent $MyInvocation.MyCommand.Path)\SemVer.ps1"

function Write-Log {
    [CmdletBinding()]
    Param
    (
        [Parameter(Position=0, Mandatory=$true, ValueFromPipelineByPropertyName=$true)]
        [ValidateNotNullOrEmpty()]
        [Alias("M")]
        [string]$Message,

        [Parameter(Mandatory=$false)]
        [Alias("P")]
        [string]$Path,

        [Parameter(Mandatory=$false)]
        [Alias("L")]
        [ValidateSet("Error","Warn","Info","Debug","Default")]
        [string]$Level="Default",

        [Parameter(Mandatory=$false)]
        [Alias("N")]
        [switch]$DoNotOverwrite
    )

    Begin {
        $VerbosePreference = 'Continue'
        $DebugPreference = 'Continue'
    }
    Process {
        $FormattedDate = (Get-Date).ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ssZ")
        $LogMessage = "[$FormattedDate] [$($Level.ToUpper().PadRight(7, ' '))] $Message"
        switch ($Level)
        {
            'Error' {
                Write-Error $LogMessage | Out-Null
            }
            'Warn' {
                Write-Warning $LogMessage | Out-Null
            }
            'Info' {
                Write-Verbose $LogMessage | Out-Null
            }
            'Debug' {
                Write-Debug $LogMessage | Out-Null
            }
            'Default' {
                Write-Output $LogMessage | Out-Null
            }
        }

        if ($Path) {
            if ((Test-Path $Path) -and $DoNotOverwrite) {
                Write-Error "Log file $Path already exists but you specified not to overwrite. Either delete the file or specify a different name." | Out-Null
                return
            }
            elseif (!(Test-Path $Path)) {
                Write-Verbose "Creating $Path." | Out-Null
                New-Item $Path -Force -ItemType File | Out-Null
            }
            $LogMessage | Out-File -FilePath $Path -Append
        }
    }
    End {
    }
}

Set-Alias -Name log -Value Write-Log -Description "logs a message to console and optionally to file"

function Set-Preferences() {
    log "setting Debug Mode" -l Debug
    $Global:DebugPreference = "Continue"
    $Global:VerbosePreference = "Continue"
    Set-StrictMode -version Latest
}

function ReplaceInFile($File, $Replacements) {
    $content = Get-Content $File
    
    foreach($key in $Replacements.Keys) {
        $content = $content.Replace($key, $Replacements.$key)
    }

    Set-Content -Path $File -Value $content
}

function Get-RandomName($Count=60) {
    $start = -join ([char][int]((97..122) | Get-Random))
    $middle = -join (0..$Count|%{[char][int]((97..122) + (48..57) +(45) | Get-Random)})
    $end = -join ([char][int]((97..122) + (48..57) | Get-Random))
    return $start + $middle + $end
}

function Get-ProgramFilesFolder() {
    return ([Environment]::GetEnvironmentVariable("ProgramW6432"), [Environment]::GetFolderPath("ProgramFiles") -ne $null)[0]
}

function Set-RemoteSignedExecutionPolicy() {
    $policy = "RemoteSigned"
    $current = Get-ExecutionPolicy
    if ($current -ne $policy)
    {
        log "setting execution policy to $policy, currently $current" -l Debug
        Set-ExecutionPolicy $policy -Force
    }
}

function Add-Chocolatey() {
    $choco = where.exe choco 2>$null
    if (!$choco) {
        log "chocolatey not installed on machine. installing now" -l Debug
        Set-RemoteSignedExecutionPolicy
        Invoke-WebRequest https://chocolatey.org/install.ps1 -UseBasicParsing | Invoke-Expression
        RefreshEnv | Out-Null
    }
}

function Test-AtlasToken() {
    $token = Get-ChildItem env:ATLAS_TOKEN
    if (!$token) {
        log "No ATLAS_TOKEN environment variable detected. Ensure that you have set up an account on https://atlas.hashicorp.com and generate an access token at https://atlas.hashicorp.com/settings/tokens" -l Error
        Exit 1
    }
}

function Test-HyperV() {

	$currentPrincipal = [Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()

	if (-not ($currentPrincipal.IsInRole(([Security.Principal.WindowsBuiltInRole]::Administrator)))) {
		log "skipping Hyper-V check as current principal is not an Administrator"
		return
	}

    log "run Hyper-V check" -l Debug

    #Requires -RunAsAdministrator
    $HyperV = Get-WindowsOptionalFeature -Online -FeatureName Microsoft-Hyper-V-All | Out-Null

    if ($HyperV -and ($HyperV.State -eq "Enabled")) {
        log "Hyper-V is enabled. If VirtualBox cannot start a VM, you _may_ need to disable this (Turn off in Windows features then restart) to allow VirtualBox to work" -l Warn
    }
    log "finished Hyper-V check" -l Debug
}

function Add-Vagrant() {
    $vagrant = where.exe vagrant 2>$null
    if (!$vagrant) {
        log "vagrant not installed on machine. installing now" -l Debug
        Add-Chocolatey
        choco install vagrant -y
        RefreshEnv | Out-Null
    }

    Add-Cygpath
}

function Add-VagrantAzureProvider() {
	$vagrantAzurePlugin = "vagrant-azure"
	$plugins = vagrant plugin list
	foreach ($plugin in $plugins) { 
		if ($plugin.Contains($vagrantAzurePlugin)) {
			log "$vagrantAzurePlugin plugin is already installed" -l Debug
			return
		}
	}
    
    $pluginVersion = "2.0.0.pre8"

	vagrant plugin install $vagrantAzurePlugin --plugin-version $pluginVersion

	# don't log the Azure SubscriptionId and Admin Username. https://github.com/Azure/vagrant-azure/issues/191
    $vagrantAzureRunInstanceFile = [Environment]::ExpandEnvironmentVariables("%USERPROFILE%\.vagrant.d\gems\2.2.5\gems\vagrant-azure-$pluginVersion\lib\vagrant-azure\action\run_instance.rb")
    $replacements = @{
        'env[:ui].info(" -- Subscription Id: #{config.subscription_id}")' = 'env[:ui].info(" -- Subscription Id: <redacted>")'
        'env[:ui].info(" -- Admin Username: #{admin_user_name}")' = 'env[:ui].info(" -- Admin Username: <redacted>")'
    }

    ReplaceInFile -File $vagrantAzureRunInstanceFile -Replacements $replacements
}

function Add-VagrantAzureBox() {
	$vagrantAzureBox = "azure"
	$boxes = vagrant box list
	foreach ($box in $boxes) { 
		if ($box.Contains($vagrantAzureBox)) {
			log "$vagrantAzureBox box exists" -l Debug
			return
		}
	}

	vagrant box add $vagrantAzureBox https://github.com/azure/vagrant-azure/raw/v2.0/dummy.box --provider azure
}

function Add-Git() {
    $gitpath = where.exe git 2>$null
    if (!$gitpath) {
        log "git not installed on machine. installing now" -l Debug
        Add-Chocolatey
        choco install git -y
        RefreshEnv | Out-Null
    }
}

# required to be in the PATH for Vagrant
function Add-Cygpath() {
    $cygpath = where.exe cygpath 2>$null
    if (!$cygpath) {
        Add-Git
        $gitpath = where.exe git
        $parentDir = $gitpath | Split-Path | Split-Path
        $cygpath = Join-Path -Path $parentDir -ChildPath "usr\bin"
        log "Adding $cygpath to PATH Environment Variable" -l Debug
        $env:path += ";$cygpath"
        RefreshEnv | Out-Null
    }
}

function Invoke-IntegrationTestsOnLocal($Location, $Version, $PreviousVersions, $VagrantProvider, $VagrantDestroy) {
    cd $Location  
    $testDirName = Split-Path $Location -Leaf
    vagrant destroy local -f

	try {
		vagrant up local    
		vagrant powershell local -c "C:\common\PesterBootstrap.ps1 -Version '$Version' -PreviousVersions @($($($PreviousVersions | ForEach-Object { "'$_'" }) -join ",")) -TestDirName '$testDirName'"
	}
	finally {
		if ($VagrantDestroy) {
			vagrant destroy local -f
		}
	}
}

function Get-WinRmSession($DnsName) {
	$name = "$DnsName.westeurope.cloudapp.azure.com"
	# using self-signed cert so skip certificate checks
	$sessionOptions = New-PSSessionOption -SkipCACheck -SkipCNCheck -SkipRevocationCheck
	$securePassword = ConvertTo-SecureString -AsPlainText -Force -String $env:AZURE_ADMIN_PASSWORD
	$credentials = New-Object -Typename System.Management.Automation.PSCredential -ArgumentList $env:AZURE_ADMIN_USERNAME, $securePassword

	return New-PSSession -ComputerName $name -Credential $credentials `
            -UseSSL -SessionOption $SessionOptions -ErrorAction "Stop"
}

function Copy-SyncedFoldersToRemote([System.Management.Automation.Runspaces.PSSession]$Session, [HashTable]$SyncFolders) {	
	if (!$SyncFolders) {
		$SyncFolders = @{
			"./../../Common/" = "/common"
      		"./../../../../../build/out/" = "/out"
      		"./" = "/vagrant"
		}
	}
	
	foreach($syncFolderKey in $SyncFolders.Keys) {
		Copy-Item -Path $syncFolderKey -Destination "C:$($SyncFolders.$syncFolderKey)" -Recurse -Force -ToSession $Session
	}
}

function Copy-SyncedFoldersFromRemote([System.Management.Automation.Runspaces.PSSession]$Session, $TestDirName, [HashTable]$SyncFolders) {
	if (!$SyncFolders) {
		$SyncFolders = @{
			"/vagrant/install.log" = "./../../../../../build/out/$TestDirName-install.log"
			"/vagrant/upgrade.log" = "./../../../../../build/out/$TestDirName-upgrade.log"
			"/vagrant/uninstall.log" = "./../../../../../build/out/$TestDirName-uninstall.log"
			"/out/*.xml" = "./../../../../../build/out"
			"/out/elasticsearch.log" = "./../../../../../build/out/$TestDirName-elasticsearch.log"
		}
	}
	
	foreach($syncFolderKey in $SyncFolders.Keys) {
		$path = "C:$syncFolderKey"
		if (Invoke-Command -Session $Session -ScriptBlock {Test-Path -Path $args[0]} -ArgumentList $path) {
			Copy-Item -Path $path -Destination "$($SyncFolders.$syncFolderKey)" -Recurse -Force -FromSession $Session
		}
	}
}

function Invoke-IntegrationTestsOnAzure($Location, $Version, $PreviousVersions) {
    cd $Location
    $dnsName = Get-RandomName
    $testDirName = Split-Path $Location -Leaf
    $resourceGroupName = $testDirName + "-" + (Get-RandomName -Count $(59 - $testDirName.Length))

    $replacements = @{
        "AZURE_DNS_NAME" = $dnsName
        "AZURE_RESOURCE_GROUP_NAME" = $resourceGroupName
    }

    ReplaceInFile -File "Vagrantfile" -Replacements $replacements
    
	try {
		vagrant destroy azure -f
		vagrant up azure
		$session = [System.Management.Automation.Runspaces.PSSession](Get-WinRmSession -DnsName $dnsName)
		Copy-SyncedFoldersToRemote -Session $session
		log "Run Pester bootstrap"	
		vagrant powershell azure -c "C:\common\PesterBootstrap.ps1 -Version '$Version' -PreviousVersions @($($($PreviousVersions | ForEach-Object { "'$_'" }) -join ",")) -TestDirName '$testDirName'"
		Copy-SyncedFoldersFromRemote -Session $session -TestDirName $testDirName
		Remove-PSSession $session
	}
	catch {
		$ErrorMessage = $_.Exception.ToString()
		log $ErrorMessage -l Error
		Exit 1
	}
	finally {
		# don't wait for the destruction
		ReplaceInFile -File "Vagrantfile" -Replacements @{ "azure.wait_for_destroy = true" = "azure.wait_for_destroy = false" }   
		vagrant destroy azure -f
    }
}

function Invoke-IntegrationTestsOnQuickAzure($Location, $Version, $PreviousVersions, $Session) {
	$currentLocation = Get-Location   
	$testDirName = Split-Path $Location -Leaf
	try {
		$SyncFolders = @{
      		"./" = "/vagrant"
		}
		cd $Location
		Invoke-Command -ScriptBlock {Remove-Item "C:\vagrant" -Recurse -Force -ErrorAction Ignore} -Session $Session
		Copy-SyncedFoldersToRemote -Session $Session -SyncFolders $SyncFolders
		log "Run Pester bootstrap"	
		cd $currentLocation

		vagrant powershell azure -c "C:\common\PesterBootstrap.ps1 -Version '$Version' -PreviousVersions @($($($PreviousVersions | ForEach-Object { "'$_'" }) -join ",")) -TestDirName '$testDirName'"
		cd $Location
		Copy-SyncedFoldersFromRemote -Session $Session -TestDirName $testDirName	
		cd $currentLocation
	}
	catch {
		$ErrorMessage = $_.Exception.ToString()
		log $ErrorMessage -l Error
	}
}

function Invoke-IntegrationTests($CurrentDir, $TestDirs, $VagrantProvider, $Version, $PreviousVersions, $Gui, $VagrantDestroy) {

	# run all tests on one vagrant box
	if ($VagrantProvider -eq "quick-azure") {
		try {
			Copy-Item "$currentDir\common\Vagrantfile" -Destination $CurrentDir -Force
			$dnsName = Get-RandomName
			$testDirName = "Quick-Tests"
			$resourceGroupName = $testDirName + "-" + (Get-RandomName -Count $(59 - $testDirName.Length))
			$replacements = @{
				"AZURE_DNS_NAME" = $dnsName
				"AZURE_RESOURCE_GROUP_NAME" = $resourceGroupName
			}
			ReplaceInFile -File "Vagrantfile" -Replacements $replacements
	
			vagrant destroy azure -f
			vagrant up azure

			$session = [System.Management.Automation.Runspaces.PSSession](Get-WinRmSession -DnsName $dnsName)
			$syncFolders = @{
				"./Common/" = "/common"
      			"./../../../build/out/" = "/out"
			}

			Copy-SyncedFoldersToRemote -Session $session -SyncFolders $syncFolders

			foreach ($dir in $TestDirs) {  
				log "running tests in $dir"
				Invoke-IntegrationTestsOnQuickAzure -Location $dir -Version "$Version" -PreviousVersions $PreviousVersions -Session $session
			}

			Remove-PSSession $session
		}
		catch {
			$ErrorMessage = $_.Exception.ToString()
			log $ErrorMessage -l Error
			Exit 1
		}
		finally {
			# don't wait for the destruction of the VM
			ReplaceInFile -File "Vagrantfile" -Replacements @{ "azure.wait_for_destroy = true" = "azure.wait_for_destroy = false" }   
			vagrant destroy azure -f
			Remove-Item "$CurrentDir\Vagrantfile" -Force -ErrorAction Ignore
		}
	}
	else {
		foreach ($dir in $TestDirs) {  
			log "running tests in $dir"
			Copy-Item "$CurrentDir\common\Vagrantfile" -Destination $dir -Force

			if ($VagrantProvider -eq "local") {
				if ($Gui) {
					ReplaceInFile -File "$dir\Vagrantfile" -Replacements @{ "#vb.gui = true" = "vb.gui = true" }
				}

				Invoke-IntegrationTestsOnLocal -Location $dir -Version $Version -PreviousVersions $PreviousVersions -VagrantDestroy:$VagrantDestroy
			} 
			else {
				Invoke-IntegrationTestsOnAzure -Location $dir -Version $Version -PreviousVersions $PreviousVersions 
			}
		}
	}
}

function Get-Installer([string] $Location = ".\..\out", $Product = "elasticsearch", $Version = $Global:Version) {
	$exePath = "$Location\$Product\$Product-$($Version.FullVersion).msi"
	log "get windows installer from $exePath" -l Debug   

    if (!(Test-Path $exePath)) {
        log "No installer found at $exePath" -l Error
    }
     
	return Get-Item $exePath
}

function Start-Fiddler($Port=8888) {
	& "$($env:LOCALAPPDATA)\Programs\Fiddler\Fiddler.exe" /noattach /noversioncheck /port:$Port
}

function Get-FiddlerSession() {	
	# Ensure the custom rules are in the Fiddler2 Scripts directory
	$scriptsDir = "$([Environment]::GetFolderPath("MyDocuments"))\Fiddler2\Scripts"
	if (-not(Test-Path $scriptsDir)) {
		New-Item $scriptsDir -Type Directory | Out-Null
	}

	if (-not(Test-Path "$scriptsDir\CustomRules.js")) {
		Copy-Item -Path "C:\common\CustomRules.js" -Destination $scriptsDir -Force
	}

	$exitCode = (Start-Process "$($env:LOCALAPPDATA)\Programs\Fiddler\ExecAction.exe" -ArgumentList "dump_session" -Wait -PassThru).ExitCode
	if ($exitCode -ne 0) {
		log "ExecAction exited with code: $exitCode" -Level Error
	}

	return Get-Content C:\session.har
}

function Stop-Fiddler() {
	Get-Process Fiddler -ErrorAction Ignore | Stop-Process
}

function Add-Quotes (
        [System.Collections.ArrayList]
        [Parameter(Position=0)]
        $Exeargs) {

    if (!$Exeargs) {
        return New-Object "System.Collections.ArrayList"
    }

    # double quote all argument values
    for ($i=0;$i -lt $Exeargs.Count; $i++) {
	    $Item = ([string]$Exeargs[$i]).Split('=')
        $Key = $Item[0]
        $Value = $Item[1]

        if (! $($Value.StartsWith("`""))) {
            $Value = "`"$Value"
        }
        if (($Value -eq "`"") -or (! $($Value.EndsWith("`"")))) {
            $Value = "$Value`""
        }
        $Exeargs[$i] = "$Key=$Value"
    }

    return $Exeargs
}

function Invoke-SilentInstall {
    [CmdletBinding()]
    Param (
        [System.Collections.ArrayList]
        [Parameter(Position=0)]
        $Exeargs,

		$Version = $Global:Version,

		[switch]
		$Upgrade
    )

	# if there are local zips of the plugins in build/out with the same version,
	# use them for installation
	if ($Exeargs) {
		$Newargs = New-Object "System.Collections.ArrayList"
		$Exeargs | %{
			if ($_.StartsWith("PLUGINS=")) {
				$plugins = $_.Replace("PLUGINS=", "").Split(',') | %{
					$plugin = $_
					$zip = ".\..\out\$plugin-$($Version.FullVersion).zip"
					if (Test-Path $zip) {
						(Get-Item $zip).FullName
					} else {
						$plugin
					}
				}
				$Newargs.Add("PLUGINS=$($plugins -join ",")") | Out-Null
			} else {
				$Newargs.Add($_) | Out-Null
			}		
		}
		$Exeargs = $Newargs
	}

	if ($Version.Source) {
        if (!$Exeargs) {
            $Exeargs = @("PLUGINSSTAGING=$($Version.Source)")
        }
        elseif (-not ($Exeargs | ?{$_ -like "PLUGINSSTAGING=*" })) {
            $Exeargs.Add("PLUGINSSTAGING=$($Version.Source)") | Out-Null
        }
    }


    $QuotedArgs = Add-Quotes $Exeargs
    $Exe = Get-Installer -Version $Version
	if ($Upgrade) {
		$logFile = "upgrade.log"
	} 
	else {
		$logFile = "install.log"
	}
	$argumentList = "/i $Exe /qn /l*v $logFile $QuotedArgs"
    log "running installer: msiexec.exe $argumentList" -Level Debug

	$global:InstallStartDate = Get-Date

    $ExitCode = (Start-Process C:\Windows\System32\msiexec.exe -ArgumentList $argumentList -Wait -PassThru).ExitCode

    if ($ExitCode) {
        log "last exit code not zero: $ExitCode" -l Error
    }

    return $ExitCode
}

function Invoke-SilentUninstall {
    [CmdletBinding()]
    Param (
        [System.Collections.ArrayList]
        [Parameter(Position=0)]
        $Exeargs,

		$Version = $Global:Version
    )

    $QuotedArgs = Add-Quotes $Exeargs
    $Exe = Get-Installer -Version $Version
	$logFile = "uninstall.log"
	
	$argumentList = "/x $Exe /qn /l*v $logFile $QuotedArgs"
    log "running installer: msiexec.exe $argumentList"
    $ExitCode = (Start-Process C:\Windows\System32\msiexec.exe -ArgumentList $argumentList -Wait -PassThru).ExitCode

    if ($ExitCode) {
        log "last exit code not zero: $ExitCode" -l Error
    }

    return $ExitCode
}

function Get-ConfigEnvironmentVariableForVersion($Version) {
	if (! ($Version)) {
		$Version = $Global:Version
	}

	# Compiled versions *always* use ES_PATH_CONF
	if ($Version.Major -eq 5 -and $Version.SourceType -ne "Compile") {
		return "ES_CONFIG"
	}
	else {
		return "ES_PATH_CONF"
	}
}

function Ping-Node([System.Timespan]$Timeout = (New-Timespan -Seconds 3), $Domain = "localhost", $Port = 9200) {
    $Result = @{
        Success = $false
        XPackSecurityInstalled = $false
    }

    $StopWatch = [Diagnostics.Stopwatch]::StartNew()
    do {
        try {
            $Response = Invoke-RestMethod "http://$($Domain):$Port" -ContentType "application/json"
            log "Elasticsearch version $($Response.version.number) running"
            $Result.Success = $true
            return $Result
        }
        catch {
            $Code = $_.Exception.Response.StatusCode.value__
            $Description = $_.Exception.Response.StatusDescription

            try {
                $Response = $_ | ConvertFrom-Json
                if ($Response -and $Response.status -and ($Response.status -eq 401)) {
                    # X-Pack Security has been set up on the node and we received an authenticated response back
                    log "Elasticsearch is running; received $Code authentication response back: $_" -l Debug
                    $Result.Success = $true
                    $Result.XPackSecurityInstalled = $true
                    return $Result
                }
            }
            catch {
                log "code: $Code, description: $Description" -l Warn
            }
        }
    } until ($StopWatch.Elapsed -gt $Timeout)

    return $Result
}

function Get-ElasticsearchService() {
    return Get-Service elasticsearch*
}

function Get-ElasticsearchWin32Service() {
    return Get-WmiObject Win32_Service | Where-Object { $_.Name -match "Elasticsearch" }
}

function Get-ElasticsearchWin32Product() {
    return Get-WmiObject Win32_Product | Where-Object { $_.Vendor -match 'Elastic' }
}

function Get-MachineEnvironmentVariable {
	[CmdletBinding()]
	Param (
		[Parameter(ValueFromPipeline)]
		[ValidateNotNullOrEmpty()]
		[string]$Name
	)

    return [Environment]::GetEnvironmentVariable($Name,"Machine")
}

function Get-TotalPhysicalMemory() {
    return (Get-WmiObject Win32_PhysicalMemory | Measure-Object -Property Capacity -Sum).Sum / 1mb
}

function Get-ElasticsearchYamlConfiguration($Version) {
	$esConfigEnvVar = Get-ConfigEnvironmentVariableForVersion -Version $Version
	$EsConfig = Get-MachineEnvironmentVariable $esConfigEnvVar
    $EsData = Split-Path $EsConfig -Parent | Join-Path -ChildPath "data"
	return Get-Content "$EsData\elasticsearch.yml"
}

function Merge-Hashtables {
    $Output = @{}
    foreach ($Hashtable in ($Input + $Args)) {
        if ($Hashtable -is [Hashtable]) {
            foreach ($Key in $Hashtable.Keys) {
                $Output.$Key = $Hashtable.$Key
            }
        }
    }
    return $Output
}

function Get-ExpectedServiceStatus($Version, $PreviousVersion) {	
	$expectedStatus = "Running"
	$552Release = ConvertTo-SemanticVersion "5.5.2"
	$600Release = ConvertTo-SemanticVersion "6.0.0"

	if ((Compare-SemanticVersion $PreviousVersion $552Release) -le 0 -and $PreviousVersion.SourceType -eq "Released" `
	    -and (Compare-SemanticVersion $Version $600Release) -le 0 -and $Version.SourceType -ne "Compile") {

		Write-Output "Previous version is $($PreviousVersion.Description) and version is $($Version.Description). Expected status is Stopped."
		$expectedStatus = "Stopped"
	}

	return $expectedStatus
}

function Get-Version() {
	$Global:Version = ConvertTo-SemanticVersion $env:Version
}

function Get-PreviousVersions() {
	if ($env:PreviousVersions) {
		$Global:PreviousVersions = $($env:PreviousVersions).Split(",") | ForEach-Object { ConvertTo-SemanticVersion $_ }
	}
	else {
		$Global:PreviousVersions = @()
	}
}

function Copy-ElasticsearchLogToOut($Path = "$(Join-Path -Path $($env:ALLUSERSPROFILE) -ChildPath "Elastic\Elasticsearch\logs\elasticsearch.log")") {
	Copy-Item -Path $Path -Destination "$($PWD.Drive.Root)out" -Force -ErrorAction Ignore
}
function Get-ChildPath {
	[CmdletBinding()]
	param(
		[Parameter(Position=0,ValueFromPipeline=$TRUE)]
		$Version = $Global:Version
	)

	# any release or build candidate before 6.0.0 won't include version in INSTALLDIR
	$600Release = ConvertTo-SemanticVersion "6.0.0"

	if ((Compare-SemanticVersion $Version $600Release) -ge 0 -or $Version.SourceType -eq "Compile") {
		$ChildPath = "Elastic\Elasticsearch\$($Version.FullVersion)\"		
	}
	else {
		$ChildPath = "Elastic\Elasticsearch\"
	}

	return $ChildPath
}

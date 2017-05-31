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
                Write-Error $LogMessage
            }
            'Warn' {
                Write-Warning $LogMessage
            }
            'Info' {
                Write-Verbose $LogMessage
            }
            'Debug' {
                Write-Debug $LogMessage
            }
            'Default' {
                Write-Output $LogMessage
            }
        }

        if ($Path) {
            if ((Test-Path $Path) -and $DoNotOverwrite) {
                Write-Error "Log file $Path already exists but you specified not to overwrite. Either delete the file or specify a different name."
                return
            }
            elseif (!(Test-Path $Path)) {
                Write-Verbose "Creating $Path."
                New-Item $Path -Force -ItemType File | Out-Null
            }
            $LogMessage | Out-File -FilePath $Path -Append
        }
    }
    End {
    }
}

Set-Alias -Name log -Value Write-Log -Description "logs a message to console and optionally to file"

function Set-DebugMode() {
    log "setting Debug Mode" -l Debug
    $Global:DebugPreference = "Continue"
    $Global:VerbosePreference = "Continue"
    Set-StrictMode -version Latest
}

function Set-ProductionMode() {
    log "setting Production Mode" -l Debug
    $Global:DebugPreference = "SilentlyContinue"
    $Global:VerbosePreference = "SilentlyContinue"
    Set-StrictMode -Off
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
    Write-Host "run Hyper-V check"
    #Requires -RunAsAdministrator
    $HyperV = Get-WindowsOptionalFeature -Online -FeatureName Microsoft-Hyper-V-All

    if ($HyperV -and ($HyperV.State -eq "Enabled")) {
        log "Hyper-V is enabled. If VirtualBox cannot start a VM, you _may_ need to disable this (Turn off in Windows features then restart) to allow VirtualBox to work" -l Warn
    }
    Write-Host "finished Hyper-V check"
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

function Invoke-IntegrationTestsOnLocal($Location, $Version, $VagrantProvider) {
    cd $Location  
    $testDirName = Split-Path $Location -Leaf
    vagrant destroy local -f

	try {
		vagrant up local    
		vagrant powershell local -c "C:\common\PesterBootstrap.ps1 -Version $Version -TestDirName '$testDirName'"
	}
	finally {
		vagrant destroy local -f
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

function Copy-SyncedFoldersToRemote([System.Management.Automation.Runspaces.PSSession]$Session) {	
	$syncFolders = @{
		"./../../Common/" = "/common"
      	"./../../../../../build/out/" = "/out"
      	"./" = "/vagrant"
	}
	
	foreach($syncFolderKey in $syncFolders.Keys) {
		Copy-Item -Path $syncFolderKey -Destination "C:$($syncFolders.$syncFolderKey)" -Recurse -Force -ToSession $Session
	}
}

function Copy-SyncedFoldersFromRemote([System.Management.Automation.Runspaces.PSSession]$Session, $TestDirName) {
	$syncFolders = @{
		"/vagrant/install.log" = "./../../../../../build/out/$TestDirName-install.log"
		"/vagrant/uninstall.log" = "./../../../../../build/out/$TestDirName-uninstall.log"
      	"/out/*.xml" = "./../../../../../build/out"
	}
	
	foreach($syncFolderKey in $syncFolders.Keys) {
		Copy-Item -Path "C:$syncFolderKey" -Destination "$($syncFolders.$syncFolderKey)" -Recurse -Force -FromSession $Session -ErrorAction Ignore
	}
}

function Invoke-IntegrationTestsOnAzure($Location, $Version) {
    cd $Location
    $dnsName = Get-RandomName
    $testDirName = Split-Path $Location -Leaf
    $resourceGroupName = $testDirName + "-" + (Get-RandomName -Count $(59 - $testDirName.Length))

    $replacements = @{
        "AZURE_DNS_NAME" = $dnsName
        "AZURE_RESOURCE_GROUP_NAME" = $resourceGroupName
    }

    ReplaceInFile -File "Vagrantfile" -Replacements $replacements
    vagrant destroy azure -f

	try {
		vagrant up azure
		$session = [System.Management.Automation.Runspaces.PSSession](Get-WinRmSession -DnsName $dnsName)
		Copy-SyncedFoldersToRemote -Session $session
		log "Run Pester bootstrap"	
		vagrant powershell azure -c "C:\common\PesterBootstrap.ps1 -Version $Version -TestDirName '$testDirName'"
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
		#vagrant destroy azure -f
    }
}

function Get-Installer([string] $Location, $Product, $Version) {
	if (!$Product) {
		$Product = "elasticsearch"
	}

	if (!$Location) {
		$Location = ".\..\out"
	}

	if (!$Version) {
		$Version = $env:EsVersion
	}

	$exePath = "$Location\$Product\$Product-$Version.msi"
	log "get windows installer from $exePath" -l Debug   

    if (!(Test-Path $exePath)) {
        log "No installer found at $exePath" -l Error
    }
     
	return Get-Item $exePath
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
        $Exeargs
    )

    $QuotedArgs = Add-Quotes $Exeargs
    $Exe = Get-Installer
    log "running installer: msiexec.exe /i $Exe /qn /l install.log $QuotedArgs"
    $ExitCode = (Start-Process C:\Windows\System32\msiexec.exe -ArgumentList "/i $Exe /qn /l install.log $QuotedArgs" -Wait -PassThru).ExitCode

    if ($ExitCode) {
        Write-Output "last exit code not zero: $ExitCode"
        log "last exit code not zero: $ExitCode" -l Error
    }

    return $ExitCode
}

function Invoke-SilentUninstall {
    [CmdletBinding()]
    Param (
        [System.Collections.ArrayList]
        [Parameter(Position=0)]
        $Exeargs
    )

    $QuotedArgs = Add-Quotes $Exeargs
    $Exe = Get-Installer
    log "running installer: msiexec.exe /x $Exe /qn /l uninstall.log $QuotedArgs"
    $ExitCode = (Start-Process C:\Windows\System32\msiexec.exe -ArgumentList "/x $Exe /qn /l uninstall.log $QuotedArgs" -Wait -PassThru).ExitCode

    if ($ExitCode) {
        Write-Host "last exit code not zero: $ExitCode"
        log "last exit code not zero: $ExitCode" -l Error
    }

    return $ExitCode
}

function Ping-Node([System.Timespan]$Timeout, $Port) {
    if (!$Timeout) {
        $Timeout = New-Timespan -Seconds 3
    }

	if (!$Port) {
		$Port = "9200"
	}

    $Result = @{
        Success = $false
        XPackSecurityInstalled = $false
    }

    $StopWatch = [Diagnostics.Stopwatch]::StartNew()
    do {
        try {
            $Response = Invoke-RestMethod "http://localhost:$Port"
            log "Elasticsearch version $($Response.version.number) running"
            $Result.Success = $true
            return $Result
        }
        catch {
            $Code = $_.Exception.Response.StatusCode.value__
            $Description = $_.Exception.Response.StatusDescription

            if ($_) {
                $Response = $_ | ConvertFrom-Json
                if ($Response -and $Response.status -and ($Response.status -eq 401)) {
                    # X-Pack Security has been set up on the node and we received an authenticated response back
                    log "Elasticsearch is running; received $Code authentication response back: $_" -l Debug
                    $Result.Success = $true
                    $Result.XPackSecurityInstalled = $true
                    return $Result
                }
            }
            else {
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

function Get-MachineEnvironmentVariable($Name) {
    return [Environment]::GetEnvironmentVariable($Name,"Machine")
}

function Get-TotalPhysicalMemory() {
    return (Get-WmiObject Win32_PhysicalMemory | Measure-Object -Property Capacity -Sum).Sum / 1mb
}

function Get-ElasticsearchYamlConfiguration() {
	$EsConfig = Get-MachineEnvironmentVariable "ES_CONFIG"
    $EsData = Split-Path $EsConfig -Parent | Join-Path -ChildPath "data"
	return Get-Content "$EsData\elasticsearch.yml"
}

function Add-XPackSecurityCredentials($Username, $Password, $Roles) {
    if (!$Roles) {
        $Roles = @("superuser")
    }

    $Service = Get-ElasticsearchService
    $Service | Stop-Service

    $EsHome = Get-MachineEnvironmentVariable "ES_HOME"
    $EsConfig = Get-MachineEnvironmentVariable "ES_CONFIG"
    $EsUsersBat = Join-Path -Path $EsHome -ChildPath "bin\x-pack\users.bat"
    $ConcatRoles = [string]::Join(",", $Roles)

    # path.conf has to be double quoted to be passed as the complete argument for -E in PowerShell AND the value
    # itself has to also be double quoted to be passed to the batch script, so the inner double quotes need to be
    # escaped
    $ExitCode = & "$EsUsersBat" useradd $Username -p $Password -r $ConcatRoles -E "`"path.conf=$EsConfig`""
    if ($ExitCode) {
        throw "Last exit code : $ExitCode"
    }

    $Service | Start-Service
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
<#
	An artifact, containing following properties:
	Product: elasticsearch, kibana
	Major, Minor, Patch, Prerelease, FullVersion
	BuildId: If a Snapshot or Staging Build
	Source: Official, Staging, Snapshot
	Distribution: Msi, Zip
	Description: for the source
#>
function ConvertTo-Artifact ($version) {
	$version -match "^(?:(?<Product>\w*)\:)?(?<Version>(?<Major>\d+)\.(?<Minor>\d+)\.(?<Patch>\d+)(?:\-(?<Prerelease>[\w\-]+))?)(?:\:(?<Source>\w*))?(?:\:(?<Distribution>\w*))?(?:\:(?<BuildId>\w*))?$" | Out-Null
    $product = $matches['Product']
	$major = [int]$matches['Major']
    $minor = [int]$matches['Minor']
    $patch = [int]$matches['Patch']
	$pre = $matches['Prerelease']
	$source = $matches['Source']
	$distribution = $matches['Distribution']
	$buildId = $matches['BuildId']
	$fullVersion = $matches['Version']
	
	$description = "$product $fullVersion"
	if ($buildId) {
		$description += " $buildId"
	}
	$description += " $source $distribution"
	
    New-Object PSObject -Property @{ 
		Product = $product
        Major = $major
        Minor = $minor
        Patch = $patch
        Prerelease = $pre
		BuildId = $buildId
		Source = $source
		Distribution = $distribution
        FullVersion = $fullVersion
		Description = $description
    }
}

function Compare-Artifact($a, $b){
    $result = 0
    $result =  $a.Major.CompareTo($b.Major)
    if($result -ne 0) {
		return $result
	}

    $result = $a.Minor.CompareTo($b.Minor)
    if($result -ne 0) {
		return $result
	}

    $result = $a.Patch.CompareTo($b.Patch)
    if($result -ne 0) {
		return $result
	}

	if ($a.Prerelease -eq $null -and $b.Prerelease -eq $null) {
		return 0
	}
	
	if ($a.Prerelease -eq $null -and $b.Prerelease -ne $null) {
		return 1
	}

	if ($a.Prerelease -ne $null -and $b.Prerelease -eq $null) {
		return -1
	}

	return $a.Prerelease.CompareTo($b.Prerelease)
}
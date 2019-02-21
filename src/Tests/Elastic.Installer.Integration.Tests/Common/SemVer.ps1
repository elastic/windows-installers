<#
Powershell script port of SemVer from https://gist.github.com/jageall/c5119d5ba26fa33602d1
of https://github.com/maxhauser/semver

Copyright (c) 2013 Max Hauser 

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
#>

# TODO: Rename to Artifact
function ConvertTo-SemanticVersion ($version) {
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
	
	switch ($source) {
		('Official') {
			$description = "Official release"
		}
		('Staging') {
			$description = "Staging Build candidate for official release"
		}
		('Snapshot') {
			$description = "Snapshot On demand or nightly build"
		}	
	}
	
	switch ($distribution) {
		('Zip') {
			$description += " from zip"
		}
		('Msi') {
			$description += " from msi"
		}
	}
	
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

function Compare-SemanticVersion($a, $b){
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
	
	if ($a.Prerelease -eq "" -and $b.Prerelease -ne "") {
		return 1
	}

	if ($a.Prerelease -ne "" -and $b.Prerelease -eq "") {
		return -1
	}

    return $a.Prerelease.CompareTo($b.Prerelease)
}

function Add-RankToSemanticVersion($versions){
    for($i = 0; $i -lt $versions.Length; $i++){
        $rank = 0
        for($j = 0; $j -lt $versions.Length; $j++){
            $diff = 0
            $diff = Compare-SemanticVersion $versions[$i] $versions[$j]
            if($diff -gt 0) {
                # Write-Host $versions[$i].FullVersion "is greater than " $versions[$j].FullVersion " got diff " $diff
                $rank++
            }
        }
        $current = [PsObject]$versions[$i]
        Add-Member -InputObject $current -MemberType NoteProperty -Name Rank -Value $rank
    }
    return $versions
}
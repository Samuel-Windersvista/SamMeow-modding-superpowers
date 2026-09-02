param (
	[string]$AssemblyName,
    [string]$Configuration,
	[string]$Framework
)

$scriptDir = $PSScriptRoot
$sourceFilesRelease = @("$AssemblyName.dll")
$sourceFilesDebug = @("$AssemblyName.dll", "$AssemblyName.pdb", "$AssemblyName.dll.mdb")
$activeSourceFiles
$sptInstallPath = "C:\Games\SPT 4.0.0"
$sptClientModsPath = Join-Path $sptInstallPath "BepInEx\plugins"
$pdb2mdb = Join-Path $scriptDir "pdb2mdb.exe"

Write-Host ""

# Determine mod output path based on build configuration
switch ($Configuration) {
    'Debug'   { 
		$modOutput = Join-Path $scriptDir "bin\Debug\$Framework"
		$dllPath = Join-Path $modOutput $sourceFilesDebug[0]
		
		# Convert pdb to mdb
		Write-Host "Running pdb2mdb on $dllPath..."
		& $pdb2mdb $dllPath
		
		Write-Host "Created *.mdb file using pdb2mdb"
		Write-Host "----------------------------------------------------------------------------------------------------"
		
		$activeSourceFiles = $sourceFilesDebug
	}
    'Release' { 
		$modOutput = Join-Path $scriptDir "bin\Release\$Framework"
		$activeSourceFiles = $sourceFilesRelease
	}
    default   {
        Write-Host "Unknown configuration: $Configuration"
        exit 1
    }
}

# Ensure target directory exists
if (-not (Test-Path $sptClientModsPath)) {
	Write-Host ""
    Write-Host "Target directory not found. Creating: $sptClientModsPath"
    New-Item -ItemType Directory -Path $sptClientModsPath -Force | Out-Null
}

try {
	# Remove existing target file if it exists
	foreach ($existingFile in $sourceFilesDebug) {
		$targetFilePath = Join-Path $sptClientModsPath $existingFile
		
		if (Test-Path $targetFilePath) {
			Write-Host "Deleting existing file: $targetFilePath"
			Remove-Item $targetFilePath -Force -ErrorAction Stop
		}
	}
	
    foreach ($file in $activeSourceFiles) {
		$sourceFilePath = Join-Path $modOutput $file
		$targetFilePath = Join-Path $sptClientModsPath $file
		
		# Copy file if it exists at source
		if (Test-Path $sourceFilePath) {
			Write-Host "Copying file from: $sourceFilePath"
            Write-Host "to:  $targetFilePath"
			Write-Host ""
			Copy-Item -Path $sourceFilePath -Destination $sptClientModsPath -Force -ErrorAction Stop
		} else {
			Write-Warning "Source file not found, skipping: $sourceFilePath"
		}
	}
} catch {
    Write-Host "Copy failed: $_"
}
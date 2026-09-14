# Modding Standard phase-2 mechanical checker.
#
# Scans a single mod source directory against the machine-checkable subset of
# knowledge/spt-kb/curated/modding-standard/ (~25 rules). Each rule reports
# PASS / FAIL / SKIP / WAIVED. Exit code 1 if any unwaived FAIL exists.
#
# Conventions (stable interface for future tooling):
#   - Rule IDs are the anchors: STD-<DOMAIN>-<NNN>.
#   - Scaffold placeholders {{LIKE_THIS}} demote value-shape checks to SKIP
#     (instantiate the template first, then re-run).
#   - Waivers: a waiver file (default <mod>/MODDING-STD-WAIVER.md) with lines
#       Waiver: STD-XXX-NNN: <reason>
#     downgrades a FAIL to WAIVED.
#
# Usage:
#   scripts/check-mod-standard.ps1 -ModPath templates/server-mod
#   scripts/check-mod-standard.ps1 -ModPath <mod> -TargetSptVersion 4.1.5 -Kind client

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$ModPath,
    [string]$TargetSptVersion = "4.1.5",
    [ValidateSet("auto", "server", "client")][string]$Kind = "auto",
    [string]$WaiverFile = ""
)

$ErrorActionPreference = "Stop"

$modRoot = (Resolve-Path -LiteralPath $ModPath).Path
if (-not (Test-Path -LiteralPath $modRoot -PathType Container)) {
    Write-Error "ModPath is not a directory: $ModPath"
}

if ($WaiverFile -eq "") { $WaiverFile = Join-Path $modRoot "MODDING-STD-WAIVER.md" }
$waivers = @{}
if (Test-Path -LiteralPath $WaiverFile) {
    $wt = [System.IO.File]::ReadAllText($WaiverFile, [System.Text.Encoding]::UTF8)
    foreach ($m in [regex]::Matches($wt, '(?im)^\s*Waiver:\s*(STD-[A-Z]+-\d{3})\s*:\s*(.+?)\s*$')) {
        $waivers[$m.Groups[1].Value] = $m.Groups[2].Value
    }
}

# ---- collect sources --------------------------------------------------------

$files = @(Get-ChildItem -LiteralPath $modRoot -Recurse -File -Force |
    Where-Object { $_.FullName -notmatch '[\\/]bin[\\/]|[\\/]obj[\\/]|[\\/]node_modules[\\/]' })
$csFiles = @($files | Where-Object { $_.Extension -eq ".cs" })
$projFiles = @($files | Where-Object { $_.Extension -eq ".csproj" })

$sbSrc = New-Object System.Text.StringBuilder
foreach ($f in $csFiles) { [void]$sbSrc.AppendLine([System.IO.File]::ReadAllText($f.FullName, [System.Text.Encoding]::UTF8)) }
$src = $sbSrc.ToString()

$sbProj = New-Object System.Text.StringBuilder
foreach ($f in $projFiles) { [void]$sbProj.AppendLine([System.IO.File]::ReadAllText($f.FullName, [System.Text.Encoding]::UTF8)) }
$proj = $sbProj.ToString()

# ---- monorepo awareness (STD-STRUCT-004) ------------------------------------
# Paired/mono repos centralize Version + install paths in a root
# Directory.Build.props and share README/LICENSE at the repo root. Treat the
# nearest ancestor carrying Directory.Build.props or a .sln as the repo root.

$repoDir = $modRoot
$parentDir = Split-Path $modRoot -Parent
if (-not (Test-Path -LiteralPath (Join-Path $modRoot "Directory.Build.props"))) {
    if ((Test-Path -LiteralPath (Join-Path $parentDir "Directory.Build.props")) -or
        (Get-ChildItem -LiteralPath $parentDir -Filter "*.sln" -File -ErrorAction SilentlyContinue).Count -gt 0) {
        $repoDir = $parentDir
    }
}

$propsText = ""
$propsFile = Join-Path $repoDir "Directory.Build.props"
if (Test-Path -LiteralPath $propsFile) {
    $propsText = [System.IO.File]::ReadAllText($propsFile, [System.Text.Encoding]::UTF8)
}

function Test-RepoFile {
    param([string]$Name)
    return (Test-Path -LiteralPath (Join-Path $modRoot $Name)) -or
           (Test-Path -LiteralPath (Join-Path $repoDir $Name))
}

# ---- kind detection ---------------------------------------------------------

$isServer = $src -match 'IModMetadata'
$isClient = $src -match 'BaseUnityPlugin|BepInPlugin'
if ($Kind -eq "server") { $isServer = $true; $isClient = $false }
if ($Kind -eq "client") { $isClient = $true; $isServer = $false }
if (-not $isServer -and -not $isClient) {
    Write-Error "cannot detect mod kind in $modRoot (no IModMetadata / BaseUnityPlugin found)"
}
$kindLabel = if ($isServer -and $isClient) { "both" } elseif ($isServer) { "server" } else { "client" }

# ---- helpers ----------------------------------------------------------------

$script:counts = @{ pass = 0; fail = 0; skip = 0; waived = 0 }
$script:failIds = New-Object System.Collections.Generic.List[string]

function Emit {
    param([string]$Id, [string]$Status, [string]$Detail = "")
    $marker = @{ PASS = "[PASS]"; FAIL = "[FAIL]"; SKIP = "[SKIP]"; WAIVED = "[WAIV]" }[$Status]
    $line = "{0} {1}" -f $marker, $Id
    if ($Detail -ne "") { $line += "  --  $Detail" }
    Write-Host $line
    $counter = switch ($Status) { "PASS" { "pass" } "FAIL" { "fail" } "SKIP" { "skip" } "WAIVED" { "waived" } }
    $script:counts[$counter]++
    if ($Status -eq "FAIL") { $script:failIds.Add($Id) }
}

function Resolve-Status {
    param([string]$Id, [bool]$Ok, [string]$Detail = "", [string]$SkipReason = "")
    if ($SkipReason -ne "") { Emit $Id "SKIP" $SkipReason; return }
    if ($Ok) { Emit $Id "PASS" $Detail; return }
    if ($waivers.ContainsKey($Id)) { Emit $Id "WAIVED" ("waived: " + $waivers[$Id]); return }
    Emit $Id "FAIL" $Detail
}

function Test-Placeholder { param([string]$Value) return $Value -match '\{\{[A-Z_]+\}\}' }

function Get-ProjValue {
    param([string]$Name)
    $m = [regex]::Match($proj, "<$Name>\s*([^<]+?)\s*</$Name>")
    if ($m.Success) { return @{ Value = $m.Groups[1].Value; Source = "csproj" } }
    $m2 = [regex]::Match($propsText, "<$Name>\s*([^<]+?)\s*</$Name>")
    if ($m2.Success) { return @{ Value = $m2.Groups[1].Value; Source = "Directory.Build.props" } }
    return $null
}

$metaFile = $csFiles | Where-Object { $_.Name -eq "ModMetadata.cs" } | Select-Object -First 1
$meta = if ($metaFile) { [System.IO.File]::ReadAllText($metaFile.FullName, [System.Text.Encoding]::UTF8) } else { "" }

# ---- checks: both kinds -----------------------------------------------------

# STD-STRUCT-002: no TypeScript/JavaScript sources
$js = @($files | Where-Object { $_.Extension -in ".ts", ".js", ".tsx", ".jsx" })
Resolve-Status "STD-STRUCT-002" ($js.Count -eq 0) ("forbidden JS/TS files: " + $js.Count)

# STD-STRUCT-005: README present (mod dir or monorepo root)
Resolve-Status "STD-STRUCT-005" (Test-RepoFile "README.md")

# STD-STRUCT-006: LICENSE present (mod dir or monorepo root)
Resolve-Status "STD-STRUCT-006" ((Test-RepoFile "LICENSE") -or (Test-RepoFile "LICENSE.md"))

# STD-META-005: three-segment semver in csproj <Version> (or root props)
$projVer = Get-ProjValue "Version"
if ($null -eq $projVer) { Resolve-Status "STD-META-005" $false "no <Version> in csproj or Directory.Build.props" }
elseif (Test-Placeholder $projVer.Value) { Resolve-Status "STD-META-005" $false "" "placeholder version '$($projVer.Value)' ($($projVer.Source)), instantiate then re-check" }
else { Resolve-Status "STD-META-005" ($projVer.Value -match '^\d+\.\d+\.\d+$') "version='$($projVer.Value)' ($($projVer.Source))" }

# STD-BUILD-005: output path without target framework
Resolve-Status "STD-BUILD-005" ($proj -match '<AppendTargetFrameworkToOutputPath>\s*false\s*<')

# STD-BUILD-006: overridable SPT install path (csproj or root props)
Resolve-Status "STD-BUILD-006" (($proj -match '<SPTInstallPath\s+Condition=') -or ($propsText -match '<SPTInstallPath\s+Condition='))

# STD-LOG-001: no Console.Write* logging
Resolve-Status "STD-LOG-001" ($src -notmatch 'Console\.Write')

# ---- checks: server ----------------------------------------------------------

if ($isServer) {
    # STD-BUILD-001: server targets net10.0
    $tf = Get-ProjValue "TargetFramework"
    Resolve-Status "STD-BUILD-001" ($tf.Value -eq "net10.0") "TargetFramework='$($tf.Value)' ($($tf.Source))"

    # STD-BUILD-004: SPTarkov core references
    $missing = @("SPTarkov.Server.Core", "SPTarkov.DI", "SPTarkov.Common" | Where-Object { $proj -notmatch [regex]::Escape($_) })
    Resolve-Status "STD-BUILD-004" ($missing.Count -eq 0) ($(if ($missing) { "missing: " + ($missing -join ",") } else { "core refs ok" }))

    # STD-META-001: exactly one IModMetadata implementation
    $metaImpls = [regex]::Matches($src, ':\s*IModMetadata').Count
    Resolve-Status "STD-META-001" ($metaImpls -eq 1) "IModMetadata implementations=$metaImpls"

    # STD-META-002: metadata lives in ModMetadata.cs
    Resolve-Status "STD-META-002" ($metaFile -and $meta -match 'IModMetadata')

    # STD-META-003: reverse-domain ModGuid
    $mg = [regex]::Match($meta, 'ModGuid\s*\{[^}]*\}\s*=\s*"([^"]+)"')
    if (-not $mg.Success) { Resolve-Status "STD-META-003" $false "ModGuid default not found" }
    elseif (Test-Placeholder $mg.Groups[1].Value) { Resolve-Status "STD-META-003" $false "" "placeholder guid, instantiate then re-check" }
    else { Resolve-Status "STD-META-003" ($mg.Groups[1].Value -match '^[a-z0-9][a-z0-9-]*(\.[a-z0-9][a-z0-9-]*)+$') ("guid='$($mg.Groups[1].Value)'") }

    # STD-META-004 + STD-VER-001: tilde SptVersion range covering the target
    $sv = [regex]::Match($meta, 'SptVersion[\s\S]{0,300}?new\s*(?:Range)?\s*\("~(\d+)\.(\d+)\.(\d+)"\)')
    if (-not $sv.Success) {
        Resolve-Status "STD-META-004" $false 'no tilde Range("~x.y.z") SptVersion found'
        Resolve-Status "STD-VER-001" $false "no SptVersion range to compare"
    } else {
        Resolve-Status "STD-META-004" $true "range ~$($sv.Groups[1].Value).$($sv.Groups[2].Value).$($sv.Groups[3].Value)"
        $tv = [regex]::Match($TargetSptVersion, '^(\d+)\.(\d+)\.(\d+)$')
        if (-not $tv.Success) {
            Resolve-Status "STD-VER-001" $false "unparseable -TargetSptVersion '$TargetSptVersion'"
        } else {
            $covers = ($tv.Groups[1].Value -eq $sv.Groups[1].Value) -and ($tv.Groups[2].Value -eq $sv.Groups[2].Value) -and ([int]$tv.Groups[3].Value -ge [int]$sv.Groups[3].Value)
            Resolve-Status "STD-VER-001" $covers ("target=$TargetSptVersion vs range ~$($sv.Groups[1].Value).$($sv.Groups[2].Value).$($sv.Groups[3].Value)")
        }
    }

    # STD-VER-002: csproj <Version> == ModMetadata Version
    $mv = [regex]::Match($meta, 'Version Version\s*\{[^}]*\}\s*=\s*new\("([^"]+)"\)')
    if ($mv.Success) {
        if ((Test-Placeholder $projVer.Value) -or (Test-Placeholder $mv.Groups[1].Value)) {
            Resolve-Status "STD-VER-002" $false "" "placeholder version, instantiate then re-check"
        } else {
            Resolve-Status "STD-VER-002" ($projVer.Value -eq $mv.Groups[1].Value) ("csproj='$($projVer.Value)' metadata='$($mv.Groups[1].Value)'")
        }
    } elseif ($meta -match 'ModVersion\.Value') {
        Resolve-Status "STD-VER-002" $true "single-source version via generated ModVersion.Value (paired linkage)"
    } else {
        Resolve-Status "STD-VER-002" $false "csproj or ModMetadata version not found"
    }

    # STD-SRV-001: [Injectable] on the mod entry
    Resolve-Status "STD-SRV-001" ($src -match '\[Injectable')

    # STD-SRV-002: TypePriority written as OnLoadOrder offsets, never bare ints
    Resolve-Status "STD-SRV-002" ($src -match 'TypePriority\s*=\s*OnLoadOrder\.')

    # STD-SRV-003: async cancellable load hook
    Resolve-Status "STD-SRV-003" ($src -match 'Task OnLoadAsync\(\s*CancellationToken')

    # STD-SRV-008: ISptLogger<T> constructor injection
    Resolve-Status "STD-SRV-008" ($src -match 'ISptLogger<')

    # STD-CFG-003: config registered via IOnDIConstruct
    Resolve-Status "STD-CFG-003" ($src -match 'IOnDIConstruct')

    # STD-CFG-004: config classes must NOT carry [Injectable]
    $badCfg = @()
    foreach ($m in [regex]::Matches($src, '(?ms)\[Injectable[^\]]*\]\s*(?:public\s+)?(?:sealed\s+)?(?:class|record)\s+(\w+)')) {
        if ($m.Groups[1].Value -match '(Config|Configuration)$') { $badCfg += $m.Groups[1].Value }
    }
    Resolve-Status "STD-CFG-004" ($badCfg.Count -eq 0) ($(if ($badCfg) { "[Injectable] on config class: " + ($badCfg -join ",") } else { "config classes clean" }))

    # STD-CFG-002: runtime config file
    Resolve-Status "STD-CFG-002" (Test-Path -LiteralPath (Join-Path $modRoot "config/config.jsonc"))

    # STD-CFG-005: default config committed alongside
    Resolve-Status "STD-CFG-005" (Test-Path -LiteralPath (Join-Path $modRoot "config/defaultConfig.jsonc"))
}

# ---- checks: client ----------------------------------------------------------

if ($isClient) {
    # STD-BUILD-002: client target framework must be Mono-compatible
    $tf = Get-ProjValue "TargetFramework"
    $allowed = @("netstandard2.1", "net472", "net471", "net46", "net461", "net462", "net6.0")
    Resolve-Status "STD-BUILD-002" ($allowed -contains $tf.Value) "TargetFramework='$($tf.Value)' ($($tf.Source))"

    # STD-CLI-001: BepInEx plugin base class
    if ($src -match ': BaseUnityPlugin') { Resolve-Status "STD-CLI-001" $true "BaseUnityPlugin (BepInEx 5 / 4.1.5)" }
    elseif ($src -match 'BasePlugin') { Resolve-Status "STD-CLI-001" $true "IL2CPP BasePlugin (BepInEx 6 / 5.0)" }
    else { Resolve-Status "STD-CLI-001" $false "no BaseUnityPlugin/BasePlugin inheritance found" }

    # STD-META-006: [BepInPlugin(guid, name, version)] with three arguments
    Resolve-Status "STD-META-006" ($src -match '\[BepInPlugin\([^)]*,[^)]*,[^)]*\)')

    # STD-CLI-003: [HarmonyPatch(typeof(T), "method")] annotations
    Resolve-Status "STD-CLI-003" ($src -match '\[HarmonyPatch\(\s*typeof\(')

    # STD-CLI-007: Harmony lifecycle - create+PatchAll, unpatch on teardown
    $hasPatch = ($src -match 'new Harmony\(') -and ($src -match '\.PatchAll\(\)')
    $hasUnpatch = ($src -match 'UnpatchSelf|UnpatchAll|\.Dispose\(\)')
    Resolve-Status "STD-CLI-007" ($hasPatch -and $hasUnpatch) ("patchAll=$hasPatch unpatch=$hasUnpatch")

    # STD-CLI-006: BepInEx log source (Logger.Log*)
    Resolve-Status "STD-CLI-006" ($src -match 'Logger\.Log(Info|Warning|Error|Debug|Message)')

    # STD-CFG-006: client config via Config.Bind (Config.Bind or configFile.Bind)
    Resolve-Status "STD-CFG-006" ($src -match '\.Bind\s*\(')
}

# ---- summary ------------------------------------------------------------------

Write-Host ""
Write-Host ("mod: {0} | kind: {1} | target: {2}" -f $modRoot, $kindLabel, $TargetSptVersion)
Write-Host ("checks: PASS={0} FAIL={1} SKIP={2} WAIVED={3} (waivers from {4})" -f $script:counts["pass"], $script:counts["fail"], $script:counts["skip"], $script:counts["waived"], $WaiverFile)

if ($script:counts["fail"] -gt 0) {
    Write-Host ("FAILED rules: " + ($script:failIds -join ", "))
    exit 1
}
exit 0

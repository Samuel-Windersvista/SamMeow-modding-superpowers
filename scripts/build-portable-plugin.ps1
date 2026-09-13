#requires -Version 5.1
<#
.SYNOPSIS
  Materialize a portable plugins/<name>/ tree for downstream packaging.

.DESCRIPTION
  Builds a self-contained, hand-distributable copy of the plugin into
  <OutputDir>/<PluginName>/. The output contains only real files (no
  directory junctions, no machine-specific absolute paths), so it can be
  zipped, committed to a release branch, or dropped into a marketplace
  cache without further surgery.

  Source-of-truth files (read from repo root):
    .opencode/plugins/      (OpenCode plugin entrypoint)
    scripts/                (operator scripts)
    skills/                 (every shipped SKILL.md tree)
    tools/mo2-mcp/          (dist/ + src/ + package.json + README.md)
    tools/spt-mcp/          (dist/ + src/ + package.json + README.md)
    tools/mo2-vfs-launcher/         (PowerShell launcher surface)
    tools/mo2-control-plane/        (broker + live-bridge Python plugin)
    tools/mo2-assets-engine/        (offline archive/loose-file engine)
    tools/mo2-mcp-sidecar/          (Python JSON-RPC sidecar)
    package.json, README.md, LICENSE, RELEASE-NOTES.md

  The script does NOT:
    - run `npm install` or `npm run build` (run those first)
    - bundle dev dependencies into the output (runtime dependency closures are
      copied from each source MCP package's node_modules; package.json files
      still have build/test scripts and devDependencies stripped)
    - mutate the live repo-root plugins/ workaround tree

.PARAMETER OutputDir
  Where the portable tree is written. Default: "dist/portable-plugin".
  Relative paths resolve from the repo root.

.PARAMETER PluginName
  Subdirectory name inside OutputDir. Default: "spt-modding-superpowers".

.PARAMETER EmitMarketplace
  Also write OutputDir/marketplace.json shaped for a marketplace cache,
  pointing at ./PluginName. Default: $true.

.PARAMETER Force
  If OutputDir/PluginName exists, remove it before writing.

.EXAMPLE
  pwsh scripts/build-portable-plugin.ps1

  Produces dist/portable-plugin/spt-modding-superpowers/ + dist/portable-plugin/marketplace.json.

.NOTES
  Inputs that MUST exist before running:
    tools/mo2-mcp/dist/index.js  (run `npm run build` inside tools/mo2-mcp/ first)
    tools/spt-mcp/dist/index.js  (run `npm run build` inside tools/spt-mcp/ first)
#>

[CmdletBinding()]
param(
  [string]$OutputDir = "dist/portable-plugin",
  [string]$PluginName = "spt-modding-superpowers",

  [bool]$EmitMarketplace = $true,

  [switch]$Force
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

# ---- Resolve repo root from this script's location -------------------------
$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path

# ---- Resolve OutputDir relative to repo root if not rooted -----------------
if (-not [IO.Path]::IsPathRooted($OutputDir)) {
  $OutputDir = Join-Path $RepoRoot $OutputDir
}
$PluginRoot = Join-Path $OutputDir $PluginName

Write-Host "[build-portable-plugin] repo root:  $RepoRoot"
Write-Host "[build-portable-plugin] output dir: $OutputDir"
Write-Host "[build-portable-plugin] plugin:     $PluginName"

# ---- Preflight: required prebuilt artifacts --------------------------------
$RequiredArtifacts = @(
  "tools/mo2-mcp/dist/index.js",
  "tools/spt-mcp/dist/index.js"
)
foreach ($rel in $RequiredArtifacts) {
  $full = Join-Path $RepoRoot $rel
  if (-not (Test-Path -LiteralPath $full)) {
    throw "Required artifact missing: $rel. " +
          "If this is an MCP dist, run `npm run build` inside that tools/<mcp>/ package first."
  }
}

# ---- Prepare output ---------------------------------------------------------
if (Test-Path -LiteralPath $PluginRoot) {
  if ($Force) {
    Write-Host "[build-portable-plugin] removing existing $PluginRoot"
    Remove-Item -LiteralPath $PluginRoot -Recurse -Force
  } else {
    throw "$PluginRoot already exists. Pass -Force to overwrite."
  }
}
New-Item -ItemType Directory -Path $PluginRoot -Force | Out-Null

# ---- Helpers ----------------------------------------------------------------
function Copy-Tree {
  param(
    [Parameter(Mandatory)][string]$From,
    [Parameter(Mandatory)][string]$To,
    [string[]]$ExcludeNames = @()
  )
  $srcFull = Join-Path $RepoRoot $From
  if (-not (Test-Path -LiteralPath $srcFull)) {
    throw "Source not found: $From"
  }
  # Resolve any directory junctions / symlinks at the source itself.
  $resolvedSrc = (Get-Item -LiteralPath $srcFull).Target
  if ($resolvedSrc) {
    $srcFull = $resolvedSrc
  }
  $dstFull = Join-Path $PluginRoot $To
  $dstParent = Split-Path $dstFull -Parent
  if (-not (Test-Path -LiteralPath $dstParent)) {
    New-Item -ItemType Directory -Path $dstParent -Force | Out-Null
  }
  if ($ExcludeNames.Count -gt 0) {
    Copy-Item -LiteralPath $srcFull -Destination $dstFull -Recurse -Force `
      -Exclude $ExcludeNames
  } else {
    Copy-Item -LiteralPath $srcFull -Destination $dstFull -Recurse -Force
  }
}

function Copy-FileOnly {
  param(
    [Parameter(Mandatory)][string]$From,
    [Parameter(Mandatory)][string]$To
  )
  $srcFull = Join-Path $RepoRoot $From
  if (-not (Test-Path -LiteralPath $srcFull)) {
    throw "Source file not found: $From"
  }
  $dstFull = Join-Path $PluginRoot $To
  $dstParent = Split-Path $dstFull -Parent
  if (-not (Test-Path -LiteralPath $dstParent)) {
    New-Item -ItemType Directory -Path $dstParent -Force | Out-Null
  }
  Copy-Item -LiteralPath $srcFull -Destination $dstFull -Force
}

function Strip-PortableMcpPackageJson {
  param(
    [Parameter(Mandatory)][string]$PackageJsonPath
  )

  $pkg = Get-Content -LiteralPath $PackageJsonPath -Raw -Encoding UTF8 | ConvertFrom-Json
  if ($pkg.scripts) {
    foreach ($scriptKey in @("prepare", "build", "test", "test:watch", "test:integration", "typecheck")) {
      if ($pkg.scripts.PSObject.Properties.Name -contains $scriptKey) {
        $pkg.scripts.PSObject.Properties.Remove($scriptKey)
      }
    }
  }
  if ($pkg.PSObject.Properties.Name -contains "devDependencies") {
    $pkg.PSObject.Properties.Remove("devDependencies")
  }
  $pkgOut = ($pkg | ConvertTo-Json -Depth 10).Replace("`r`n", "`n")
  [IO.File]::WriteAllText($PackageJsonPath, $pkgOut + "`n", [Text.UTF8Encoding]::new($false))
}

function Copy-McpPackage {
  param(
    [Parameter(Mandatory)][string]$PackageName
  )

  $dst = Join-Path $PluginRoot "tools/$PackageName"
  New-Item -ItemType Directory -Path $dst -Force | Out-Null
  Copy-FileOnly -From "tools/$PackageName/package.json" -To "tools/$PackageName/package.json"
  if (Test-Path -LiteralPath (Join-Path $RepoRoot "tools/$PackageName/README.md")) {
    Copy-FileOnly -From "tools/$PackageName/README.md" -To "tools/$PackageName/README.md"
  }
  if (Test-Path -LiteralPath (Join-Path $RepoRoot "tools/$PackageName/tsconfig.json")) {
    Copy-FileOnly -From "tools/$PackageName/tsconfig.json" -To "tools/$PackageName/tsconfig.json"
  }
  Copy-Tree -From "tools/$PackageName/dist" -To "tools/$PackageName/dist"
  Copy-Tree -From "tools/$PackageName/src" -To "tools/$PackageName/src"
  Strip-PortableMcpPackageJson -PackageJsonPath (Join-Path $PluginRoot "tools/$PackageName/package.json")
  Copy-McpRuntimeDependencies -PackageName $PackageName
}

function Copy-McpRuntimeDependencies {
  param(
    [Parameter(Mandatory)][string]$PackageName
  )

  $srcPkgRoot = Join-Path $RepoRoot "tools/$PackageName"
  $srcNodeModules = Join-Path $srcPkgRoot "node_modules"
  if (-not (Test-Path -LiteralPath $srcNodeModules)) {
    throw "Source runtime dependencies missing for tools/$PackageName. Run npm install inside tools/$PackageName before building the portable tree."
  }

  Push-Location $srcPkgRoot
  try {
    $depRoots = @(& npm ls --omit=dev --parseable --all --silent)
    if ($LASTEXITCODE -ne 0) {
      throw "npm ls --omit=dev failed for tools/$PackageName with exit code $LASTEXITCODE"
    }
  } finally {
    Pop-Location
  }

  foreach ($depRoot in $depRoots) {
    if ($depRoot -eq $srcPkgRoot) { continue }
    if (-not $depRoot.StartsWith($srcNodeModules, [StringComparison]::OrdinalIgnoreCase)) { continue }
    $relativeDepPath = $depRoot.Substring($srcPkgRoot.Length).TrimStart([char]'\', [char]'/')
    $dstDepPath = Join-Path (Join-Path $PluginRoot "tools/$PackageName") $relativeDepPath
    $dstParent = Split-Path $dstDepPath -Parent
    if (-not (Test-Path -LiteralPath $dstParent)) {
      New-Item -ItemType Directory -Path $dstParent -Force | Out-Null
    }
    Copy-Item -LiteralPath $depRoot -Destination $dstDepPath -Recurse -Force
  }
}

# ---- 1. OpenCode plugin entrypoint -----------------------------------------
Copy-Tree -From ".opencode/plugins"  -To ".opencode/plugins"

# ---- 2. Scripts -------------------------------------------------------------
# `dev-*.ps1` scripts are repo-internal authoring helpers that assume the full
# source tree; end-users do not have it. Exclude them from the published plugin
# tree so the user-facing seam stays clean.
Copy-Tree -From "scripts" -To "scripts" -ExcludeNames "dev-*"

# ---- 3. Skills (entire shipped surface) ------------------------------------
Copy-Tree -From "skills" -To "skills"

# ---- 4. MCP packages (dist + src + package.json + README + tsconfig) --------
#       Exclude tests/ and .gitignore. Copy production node_modules closure so
#       the materialized MCP stdio entries can smoke-run without a network step.
#       Dev package.json files have `prepare: npm run build`; portable trees
#       already ship dist/ pre-built, so strip build/test scripts + dev deps.
Copy-McpPackage -PackageName "mo2-mcp"
Copy-McpPackage -PackageName "spt-mcp"

# ---- 5. tools/mo2-vfs-launcher + tools/mo2-control-plane -------------------
Copy-Tree -From "tools/mo2-vfs-launcher"  -To "tools/mo2-vfs-launcher"
Copy-Tree -From "tools/mo2-control-plane" -To "tools/mo2-control-plane"

# ---- 5a. tools/mo2-assets-engine (offline archive/loose-file engine) --------
# Bundled so the Python engine + `mo2-assets` CLI are available from the
# materialized plugin tree. Use robocopy with /XD because Python dev caches can
# appear at any depth after local test runs and must not ship to vendor clones.
$mo2AssetsEngineSrc = Join-Path $RepoRoot "tools/mo2-assets-engine"
$mo2AssetsEngineDst = Join-Path $PluginRoot "tools/mo2-assets-engine"
if (-not (Test-Path -LiteralPath $mo2AssetsEngineSrc)) {
  throw "Source not found: tools/mo2-assets-engine"
}
New-Item -ItemType Directory -Path $mo2AssetsEngineDst -Force | Out-Null
$robocopyArgs = @(
  $mo2AssetsEngineSrc,
  $mo2AssetsEngineDst,
  "/E",
  "/XD", "__pycache__", ".mypy_cache", ".pytest_cache", ".ruff_cache",
  "/XD", "*.egg-info", "build", "dist",
  "/XD", ".venv",
  "/NFL", "/NDL", "/NJH", "/NJS", "/NP"
)
& robocopy @robocopyArgs | Out-Null
# robocopy exit codes 0-7 are success variants; 8+ are real errors.
if ($LASTEXITCODE -gt 7) {
  throw "robocopy failed copying tools/mo2-assets-engine (exit $LASTEXITCODE)"
}
# Reset $LASTEXITCODE so downstream cmdlets see a clean state.
$global:LASTEXITCODE = 0

# ---- 5b. tools/mo2-mcp-sidecar (Python JSON-RPC sidecar) --------------------
# Bundled so the mo2-mcp TypeScript server can launch the sidecar from the
# materialized plugin tree. Use robocopy with /XD for Python dev caches.
$mo2SidecarSrc = Join-Path $RepoRoot "tools/mo2-mcp-sidecar"
$mo2SidecarDst = Join-Path $PluginRoot "tools/mo2-mcp-sidecar"
if (-not (Test-Path -LiteralPath $mo2SidecarSrc)) {
  throw "Source not found: tools/mo2-mcp-sidecar"
}
New-Item -ItemType Directory -Path $mo2SidecarDst -Force | Out-Null
$robocopyArgs = @(
  $mo2SidecarSrc,
  $mo2SidecarDst,
  "/E",
  "/XD", "__pycache__", ".mypy_cache", ".pytest_cache", ".ruff_cache",
  "/XD", "*.egg-info", "build", "dist",
  "/XD", ".venv",
  "/NFL", "/NDL", "/NJH", "/NJS", "/NP"
)
& robocopy @robocopyArgs | Out-Null
# robocopy exit codes 0-7 are success variants; 8+ are real errors.
if ($LASTEXITCODE -gt 7) {
  throw "robocopy failed copying tools/mo2-mcp-sidecar (exit $LASTEXITCODE)"
}
# Reset $LASTEXITCODE so downstream cmdlets see a clean state.
$global:LASTEXITCODE = 0

# ---- 6. Top-level public surface -------------------------------------------
Copy-FileOnly -From "package.json"      -To "package.json"

# The repo-root package.json `main` points at the OpenCode plugin entrypoint
# (`.opencode/plugins/spt-modding-superpowers.js`). The materialized subtree
# carries the same `.opencode/plugins/` directory, so the entry resolves
# identically inside the subtree — no rewrite is required.
$matzPkgPath = Join-Path $PluginRoot "package.json"
$matzPkg = (Get-Content -LiteralPath $matzPkgPath -Raw -Encoding UTF8) | ConvertFrom-Json
$matzPkg.main = ".opencode/plugins/spt-modding-superpowers.js"
$matzPkgOut = ($matzPkg | ConvertTo-Json -Depth 10).Replace("`r`n", "`n")
[IO.File]::WriteAllText($matzPkgPath, $matzPkgOut + "`n", [Text.UTF8Encoding]::new($false))

Copy-FileOnly -From "README.md"         -To "README.md"
Copy-FileOnly -From "LICENSE"           -To "LICENSE"
if (Test-Path -LiteralPath (Join-Path $RepoRoot "RELEASE-NOTES.md")) {
  Copy-FileOnly -From "RELEASE-NOTES.md" -To "RELEASE-NOTES.md"
}

# ---- 7. Optional sibling marketplace.json ----------------------------------
if ($EmitMarketplace) {
  $marketplace = [ordered]@{
    name = "$PluginName-portable"
    interface = [ordered]@{
      displayName = "SPT Modding Superpowers (portable)"
    }
    plugins = @(
      [ordered]@{
        name = $PluginName
        source = [ordered]@{
          source = "local"
          path = "./$PluginName"
        }
        policy = [ordered]@{
          installation = "AVAILABLE"
          authentication = "ON_INSTALL"
        }
        category = "Engineering"
      }
    )
  }
  $mpJson = ($marketplace | ConvertTo-Json -Depth 10).Replace("`r`n", "`n")
  $mpPath = Join-Path $OutputDir "marketplace.json"
  [IO.File]::WriteAllText($mpPath, $mpJson + "`n", [Text.UTF8Encoding]::new($false))
  Write-Host "[build-portable-plugin] wrote marketplace: $mpPath"
}

# ---- 8. Summary ------------------------------------------------------------
$fileCount = (Get-ChildItem -LiteralPath $PluginRoot -Recurse -File).Count
$totalBytes = (Get-ChildItem -LiteralPath $PluginRoot -Recurse -File | Measure-Object -Property Length -Sum).Sum
$totalMB = [math]::Round($totalBytes / 1MB, 2)
Write-Host ""
Write-Host "[build-portable-plugin] DONE"
Write-Host "[build-portable-plugin]   plugin root: $PluginRoot"
Write-Host "[build-portable-plugin]   files:       $fileCount"
Write-Host "[build-portable-plugin]   total size:  $totalMB MB (runtime node_modules included)"
Write-Host ""
Write-Host "Next step for consumers:"
Write-Host "  Install/copy the materialized plugin tree; MCP runtime dependencies are already included."

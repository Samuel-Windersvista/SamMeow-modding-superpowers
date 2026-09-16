#requires -Version 5.1
<#
.SYNOPSIS
  C10 pre/post golden 捕获与对照（Bridge 跨语言契约工件化）。

.DESCRIPTION
  对固定语料运行两端实现，写出 golden 并对照：
    1. 构建 tools/tarkov-runtime-mcp（dist）并运行 TS 捕获 → ts-*.json；
    2. 运行 C# 捕获装置（GoldenCaptureTests + BRIDGE_GOLDEN_DIR）→ cs-*.json；
    3. 同一阶段 CS vs TS 交叉核对（归一化 / 聚合必须逐条一致）；
    4. -Phase post 时另做 pre vs post 逐条对照（零 wire 行为变更证明）。

  语料：.scratch/c10-bridge-contract/tools/corpus/*.json（固定输入）。
  输出：.scratch/c10-bridge-contract/goldens/<phase>/。

.PARAMETER Phase
  pre（重构前基线）或 post（重构后）。

.PARAMETER SkipBuild
  跳过 npm run build（dist 已是最新时用）。

.EXAMPLE
  pwsh .scratch/c10-bridge-contract/tools/capture-goldens.ps1 -Phase pre
  pwsh .scratch/c10-bridge-contract/tools/capture-goldens.ps1 -Phase post
#>

[CmdletBinding()]
param(
  [Parameter(Mandatory = $true)]
  [ValidateSet("pre", "post")]
  [string]$Phase,

  [switch]$SkipBuild
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..\..")).Path
$ToolsDir = $PSScriptRoot
$CorpusDir = Join-Path $ToolsDir "corpus"
$GoldensRoot = Join-Path $RepoRoot ".scratch\c10-bridge-contract\goldens"
$OutDir = Join-Path $GoldensRoot $Phase

$McpRoot = Join-Path $RepoRoot "tools\tarkov-runtime-mcp"
$TestProject = Join-Path $RepoRoot "tools\tarkov-runtime-bridge\tests\TarkovRuntimeBridge.Tests"

foreach ($corpus in @("normalization-cases.json", "aggregation-cases.json")) {
  if (-not (Test-Path -LiteralPath (Join-Path $CorpusDir $corpus))) {
    throw "语料缺失：$CorpusDir\$corpus"
  }
}

New-Item -ItemType Directory -Path $OutDir -Force | Out-Null
$logPath = Join-Path $OutDir "capture.log"
$log = New-Object System.Text.StringBuilder
function Write-Log {
  param([string]$Line)
  [void]$log.AppendLine($Line)
  Write-Host $Line
}

Write-Log "[capture-goldens] repo root: $RepoRoot"
Write-Log "[capture-goldens] phase:     $Phase"
Write-Log "[capture-goldens] output:    $OutDir"

$failures = @()

# ---- 1. TS 捕获（dist） -----------------------------------------------------
if (-not $SkipBuild) {
  Write-Log "[capture-goldens] npm run build (tools/tarkov-runtime-mcp)"
  $buildOutput = & npm --prefix $McpRoot run build 2>&1
  $buildExit = $LASTEXITCODE
  Write-Log ($buildOutput -join "`n")
  if ($buildExit -ne 0) {
    throw "npm run build 失败（exit $buildExit）"
  }
}

Write-Log "[capture-goldens] TS capture"
$tsOutput = & node (Join-Path $ToolsDir "capture-ts.mjs") $OutDir 2>&1
$tsExit = $LASTEXITCODE
Write-Log ($tsOutput -join "`n")
if ($tsExit -ne 0) {
  throw "TS 捕获失败（exit $tsExit）"
}

# ---- 2. C# 捕获 -------------------------------------------------------------
Write-Log "[capture-goldens] C# capture (BRIDGE_GOLDEN_DIR)"
$previousGoldenDir = $env:BRIDGE_GOLDEN_DIR
try {
  $env:BRIDGE_GOLDEN_DIR = $OutDir
  $csOutput = & dotnet test $TestProject --filter "FullyQualifiedName~GoldenCaptureTests" --nologo 2>&1
  $csExit = $LASTEXITCODE
  Write-Log ($csOutput -join "`n")
  if ($csExit -ne 0) {
    throw "C# 捕获失败（exit $csExit）"
  }
}
finally {
  $env:BRIDGE_GOLDEN_DIR = $previousGoldenDir
}

# ---- 3. 同阶段 CS vs TS 交叉核对 --------------------------------------------
Write-Log "[capture-goldens] cross-check CS vs TS"
foreach ($pair in @(
    @{ Name = "normalization"; A = "cs-normalization.json"; B = "ts-normalization.json" },
    @{ Name = "aggregation"; A = "cs-aggregation.json"; B = "ts-aggregation.json" }
  )) {
  $aPath = Join-Path $OutDir $pair.A
  $bPath = Join-Path $OutDir $pair.B
  $diff = & node (Join-Path $ToolsDir "compare-goldens.mjs") $aPath $bPath 2>&1
  $diffExit = $LASTEXITCODE
  Write-Log ("[capture-goldens]   {0}: {1}" -f $pair.Name, ($diff -join " "))
  if ($diffExit -ne 0) {
    $failures += "CS vs TS $($pair.Name) 不一致"
  }
}

# ---- 4. pre vs post 对照 ----------------------------------------------------
if ($Phase -eq "post") {
  Write-Log "[capture-goldens] compare pre vs post"
  $PreDir = Join-Path $GoldensRoot "pre"
  foreach ($name in @(
      "cs-normalization.json", "cs-aggregation.json", "cs-payloads.json",
      "ts-normalization.json", "ts-aggregation.json"
    )) {
    $prePath = Join-Path $PreDir $name
    $postPath = Join-Path $OutDir $name
    if (-not (Test-Path -LiteralPath $prePath)) {
      $failures += "pre 缺少 $name"
      continue
    }

    $diff = & node (Join-Path $ToolsDir "compare-goldens.mjs") $prePath $postPath 2>&1
    $diffExit = $LASTEXITCODE
    Write-Log ("[capture-goldens]   {0}: {1}" -f $name, ($diff -join " "))
    if ($diffExit -ne 0) {
      $failures += "pre/post $name 不一致"
    }
  }
}

# ---- 5. 摘要 -----------------------------------------------------------------
Write-Log "[capture-goldens] sha256"
foreach ($file in (Get-ChildItem -LiteralPath $OutDir -File -Filter "*.json" | Sort-Object Name)) {
  $hash = (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash
  Write-Log ("  {0}  {1}" -f $file.Name, $hash)
}

$log.ToString() | Set-Content -LiteralPath $logPath -Encoding UTF8

if ($failures.Count -gt 0) {
  Write-Log "[capture-goldens] FAILED"
  foreach ($failure in $failures) {
    Write-Log "  - $failure"
  }
  exit 1
}

Write-Log "[capture-goldens] OK"
exit 0

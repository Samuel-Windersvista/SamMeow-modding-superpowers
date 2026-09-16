#requires -Version 5.1
<#
.SYNOPSIS
  Document-stat anchor check (C6 state authority): parse a claimed count out of a
  doc, measure the real count on disk, assert they match.

.DESCRIPTION
  Numeric claims in prose drift silently. This check pins the few counts that are
  worth keeping to their fact source, so a drift fails loudly instead of being
  discovered months later.

  Anchors (each: regex -> claimed value, filesystem -> measured value):
    a. skills count   : README.md claim == number of directories under skills/
    b. index entries  : README.md / docs/使用指南.md claim == entries in
                        knowledge/spt-kb/index.json
    c. tools count    : README.md / docs/使用指南.md claim == number of
                        subdirectories under tools/ (README.md excluded)

  When an anchor is absent from the scanned docs (the docs were minimized and no
  longer state that number) the anchor is SKIPPED with a note — absence is not a
  failure, and nothing is reported as a mismatch.

  Scan scope is deliberately limited to "current state" docs. RELEASE-NOTES.md is
  a historical release record: its dated numbers (e.g. "index.json ... 74 条" in
  the 2026-08-04 section) are timeline entries, not current claims, so it is not
  machine-checked here.

.PARAMETER RepoRoot
  Repository root. Defaults to the parent of this script's directory.

.EXAMPLE
  powershell -NoProfile -ExecutionPolicy Bypass -File scripts/verify-doc-stats.ps1
#>

[CmdletBinding()]
param(
  [string]$RepoRoot
)

$ErrorActionPreference = "Stop"

. (Join-Path $PSScriptRoot "..\tests\bootstrap\_assert.ps1")

if (-not $RepoRoot) {
  $RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
}

Write-Host "  repo root: $RepoRoot"

# ---- helpers ----------------------------------------------------------------

function Get-DocText {
  param([Parameter(Mandatory)][string]$RelativePath)

  $full = Join-Path $RepoRoot $RelativePath
  if (-not (Test-Path -LiteralPath $full)) { return $null }
  return [IO.File]::ReadAllText($full, [Text.Encoding]::UTF8)
}

function Get-MeasuredCount {
  param([Parameter(Mandatory)][scriptblock]$Measure)

  return [int](& $Measure)
}

<#
  Assert-DocStat: parse every claimed value matching $Pattern in $Docs and compare
  each against $Measure. No match -> SKIP with a note (minimized docs are valid).
#>
function Assert-DocStat {
  param(
    [Parameter(Mandatory)][string]$Label,
    [Parameter(Mandatory)][string[]]$Docs,
    [Parameter(Mandatory)][string]$Pattern,
    [Parameter(Mandatory)][scriptblock]$Measure,
    [Parameter(Mandatory)][string]$MeasuredLabel
  )

  $measured = Get-MeasuredCount -Measure $Measure
  $claims = @()

  foreach ($doc in $Docs) {
    $text = Get-DocText -RelativePath $doc
    if ($null -eq $text) { continue }

    foreach ($match in [regex]::Matches($text, $Pattern, [Text.RegularExpressions.RegexOptions]::IgnoreCase)) {
      $line = ($text.Substring(0, $match.Index) -split "`n").Count
      $claims += [pscustomobject]@{
        Doc   = $doc
        Line  = $line
        Value = [int]$match.Groups[1].Value
      }
    }
  }

  if ($claims.Count -eq 0) {
    Write-Host ("  [SKIP] {0}: no numeric anchor in docs (minimized); measured {1} = {2}" -f `
      $Label, $MeasuredLabel, $measured)
    return
  }

  foreach ($claim in $claims) {
    if ($claim.Value -ne $measured) {
      Add-BootstrapFailure ("{0} MISMATCH: {1}:{2} claims {3}, measured {4} = {5}" -f `
        $Label, $claim.Doc, $claim.Line, $claim.Value, $MeasuredLabel, $measured)
    } else {
      Write-Host ("  [OK]   {0}: {1}:{2} claims {3} == measured {4}" -f `
        $Label, $claim.Doc, $claim.Line, $claim.Value, $MeasuredLabel)
    }
  }
}

# ---- anchors ----------------------------------------------------------------

Write-Host "  anchor a: skills count (README skill table)"
Assert-DocStat `
  -Label "skills count" `
  -Docs @("README.md") `
  -Pattern '(\d+)\s*个\s*(?:SPT\s*)?skills?' `
  -Measure { (Get-ChildItem -LiteralPath (Join-Path $RepoRoot "skills") -Directory).Count } `
  -MeasuredLabel "skills/ subdir count"

Write-Host "  anchor b: index.json entry count"
Assert-DocStat `
  -Label "index.json entry count" `
  -Docs @("README.md", "docs/使用指南.md") `
  -Pattern 'index\.json[^\n]{0,40}?(\d+)\s*条' `
  -Measure {
    $json = [IO.File]::ReadAllText((Join-Path $RepoRoot "knowledge/spt-kb/index.json"), [Text.Encoding]::UTF8)
    ($json | ConvertFrom-Json).entries.Count
  } `
  -MeasuredLabel "index.json entries"

Write-Host "  anchor c: tools subproject count"
Assert-DocStat `
  -Label "tools subproject count" `
  -Docs @("README.md", "docs/使用指南.md") `
  -Pattern 'tools/[^\n]{0,30}?(\d+)\s*个子工程' `
  -Measure { (Get-ChildItem -LiteralPath (Join-Path $RepoRoot "tools") -Directory).Count } `
  -MeasuredLabel "tools/ subdir count"

Complete-BootstrapCheck -SuccessMessage "Doc-stat anchors match the filesystem (or were minimized and skipped)."

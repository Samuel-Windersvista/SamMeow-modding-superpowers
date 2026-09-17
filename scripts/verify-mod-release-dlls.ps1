# C9 · 决策 D3：release DLL 可复现性验证
#
# 对 mods/PerformanceTweaks 与 mods/PerformanceTweaks413 各重建一次，
# 用 SHA256 对照已入仓的 release/ DLL，输出对照报告。
#
# 防污染（红线：验证不得覆盖已入仓 DLL）：
#   构建时传 -p:SkipCopyToRelease=true，关闭 csproj 的 CopyToRelease target，
#   已入仓 DLL 不会被写入。构建前后各取一次 release/ 的 hash；
#   若仍发生变化，立即用 `git checkout --` 还原并判定为防污染断言失败。
#
# 退出码：
#   0 = 两 mod 重建产物与入仓 DLL 完全一致
#   1 = 检出 hash 漂移，需人工裁决（本机预期为 PE 构建溯源元数据差异；脚本不判定语义等价、不替换）
#   2 = 防污染断言失败、构建失败或产物缺失
#
# 用法：
#   powershell -NoProfile -ExecutionPolicy Bypass -File scripts/verify-mod-release-dlls.ps1
#   powershell -NoProfile -ExecutionPolicy Bypass -File scripts/verify-mod-release-dlls.ps1 -SptRoot "D:\SPT"

param(
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path,
    [string]$SptRoot = ""
)

$ErrorActionPreference = "Stop"

try { [Console]::OutputEncoding = [System.Text.Encoding]::UTF8 } catch { }

$mods = @(
    [pscustomobject]@{
        Name    = "PerformanceTweaks (SPT 3.11.x)"
        Project = "mods/PerformanceTweaks/PerformanceTweaks.csproj"
        Release = "mods/PerformanceTweaks/release/SamMeow.PerformanceTweaks.dll"
        Output  = "mods/PerformanceTweaks/bin/Release/SamMeow.PerformanceTweaks.dll"
    }
    [pscustomobject]@{
        Name    = "PerformanceTweaks413 (SPT 4.1.x)"
        Project = "mods/PerformanceTweaks413/PerformanceTweaks413.csproj"
        Release = "mods/PerformanceTweaks413/release/SamMeow.PerformanceTweaks413.dll"
        Output  = "mods/PerformanceTweaks413/bin/Release/SamMeow.PerformanceTweaks413.dll"
    }
)

function Get-Sha256 {
    param([string]$Path)
    if (-not (Test-Path -LiteralPath $Path)) { return $null }
    return (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash
}

Write-Host "============================================"
Write-Host "release DLL 可复现性验证（C9 · D3）"
Write-Host "仓库：$RepoRoot"
if ($SptRoot) { Write-Host "SptRoot 覆盖：$SptRoot" } else { Write-Host "SptRoot：使用 Directory.Build.props 默认值" }
Write-Host "============================================"

$hardFailure = $false
$drift = $false
$report = @()

foreach ($mod in $mods) {
    Write-Host ""
    Write-Host ("--- {0} ---" -f $mod.Name)

    $projectPath = Join-Path $RepoRoot $mod.Project
    $releasePath = Join-Path $RepoRoot $mod.Release
    $outputPath = Join-Path $RepoRoot $mod.Output

    if (-not (Test-Path -LiteralPath $projectPath)) {
        Write-Host ("  [FAIL] 工程不存在：{0}" -f $mod.Project)
        $hardFailure = $true
        continue
    }
    if (-not (Test-Path -LiteralPath $releasePath)) {
        Write-Host ("  [FAIL] 入仓 DLL 不存在：{0}" -f $mod.Release)
        $hardFailure = $true
        continue
    }

    $releaseHashBefore = Get-Sha256 $releasePath

    $buildArgs = @(
        "build", $projectPath, "-c", "Release", "-t:Rebuild",
        "-p:SkipCopyToRelease=true", "-v:q", "--nologo"
    )
    if ($SptRoot) { $buildArgs += "-p:SptRoot=$SptRoot" }

    $buildOutput = @(& dotnet @buildArgs 2>&1)
    $buildExit = $LASTEXITCODE
    if ($buildExit -ne 0) {
        Write-Host ("  [FAIL] 构建失败（exit {0}）" -f $buildExit)
        foreach ($line in ($buildOutput | Select-Object -Last 6)) { Write-Host ("      {0}" -f $line) }
        $hardFailure = $true
        continue
    }
    Write-Host "  [OK] 重建成功（-p:SkipCopyToRelease=true，未写 release/）"

    # 防污染断言：release/ 内容必须与构建前完全一致
    $releaseHashAfter = Get-Sha256 $releasePath
    if ($releaseHashAfter -ne $releaseHashBefore) {
        Write-Host "  [FAIL] 防污染断言失败：入仓 DLL 在验证过程中被改写，执行还原"
        & git -C $RepoRoot checkout -- $mod.Release 2>&1 | Out-Null
        $restored = Get-Sha256 $releasePath
        Write-Host ("      还原后 hash：{0}" -f $restored)
        $hardFailure = $true
        continue
    }
    Write-Host "  [OK] 防污染断言：入仓 DLL 未被触碰"

    $rebuiltHash = Get-Sha256 $outputPath
    if (-not $rebuiltHash) {
        Write-Host ("  [FAIL] 重建产物缺失：{0}" -f $mod.Output)
        $hardFailure = $true
        continue
    }

    $match = ($rebuiltHash -eq $releaseHashBefore)
    if ($match) {
        Write-Host "  [MATCH] 重建产物与入仓 DLL 一致"
    } else {
        Write-Host "  [DRIFT] 重建产物与入仓 DLL 不一致（本脚本不替换，需人工裁决）"
        $drift = $true
    }

    $report += [pscustomobject]@{
        Mod      = $mod.Name
        Verdict  = if ($match) { "MATCH" } else { "DRIFT" }
        Committed = $releaseHashBefore
        Rebuilt   = $rebuiltHash
    }
}

Write-Host ""
Write-Host "============================================"
Write-Host "SHA256 对照报告"
Write-Host "============================================"
if ($report.Count -eq 0) {
    Write-Host "  （无可用对照结果）"
} else {
    foreach ($row in $report) {
        Write-Host ("  [{0}] {1}" -f $row.Verdict, $row.Mod)
        Write-Host ("      入仓 release/：{0}" -f $row.Committed)
        Write-Host ("      重建产物    ：{0}" -f $row.Rebuilt)
    }
}

Write-Host ""
if ($hardFailure) {
    Write-Host "结论：验证未通过（防污染断言失败 / 构建失败 / 产物缺失）。"
    exit 2
}
if ($drift) {
    Write-Host "结论：检出 hash 漂移。本脚本只做 hash 对照，不判定语义等价性；"
    Write-Host "      是否替换入仓 DLL 由 Overseer 单独裁决。"
    exit 1
}
Write-Host "结论：全部一致，入仓 DLL 可复现。"
exit 0

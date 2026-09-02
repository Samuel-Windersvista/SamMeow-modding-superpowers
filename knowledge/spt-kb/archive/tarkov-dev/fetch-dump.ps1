# fetch-dump.ps1 - tarkov.dev REST dump 全量快照抓取脚本
#
# 背景：tarkov.dev 的 GraphQL 端点 (api.tarkov.dev/graphql) 因 Issue #474 故障不可用，
#       改用其 REST dump 通道 https://json.tarkov.dev（实测可用，2026-09-02 验证）。
#
# 用法：
#   powershell -ExecutionPolicy Bypass -File fetch-dump.ps1 -OutputDir .\snapshot-2026-09-02
#   powershell -ExecutionPolicy Bypass -File fetch-dump.ps1 -OutputDir .\snapshot-2026-09-02 -Force   # 覆盖已存在文件
#
# 参数：
#   -OutputDir  落盘目录（不存在则自动创建）
#   -Force      覆盖已存在的文件（默认跳过）

param(
    [Parameter(Mandatory = $true)]
    [string]$OutputDir,
    [switch]$Force
)

$ErrorActionPreference = 'Stop'

# 端点清单：[文件名, URL]
# 说明：barters/crafts 无翻译字典（translations:false）；items/tasks/hideout/maps/traders 附带 _zh/_en 翻译字典。
# 排除：prices（live 跳蚤价格历史）、status、pve/pvp-season 游戏模式。
$urls = @(
    @{ Name = 'items.json';        Url = 'https://json.tarkov.dev/regular/items' },
    @{ Name = 'items_zh.json';     Url = 'https://json.tarkov.dev/regular/items_zh' },
    @{ Name = 'items_en.json';     Url = 'https://json.tarkov.dev/regular/items_en' },
    @{ Name = 'tasks.json';        Url = 'https://json.tarkov.dev/regular/tasks' },
    @{ Name = 'tasks_zh.json';     Url = 'https://json.tarkov.dev/regular/tasks_zh' },
    @{ Name = 'tasks_en.json';     Url = 'https://json.tarkov.dev/regular/tasks_en' },
    @{ Name = 'barters.json';      Url = 'https://json.tarkov.dev/regular/barters' },
    @{ Name = 'crafts.json';       Url = 'https://json.tarkov.dev/regular/crafts' },
    @{ Name = 'hideout.json';      Url = 'https://json.tarkov.dev/regular/hideout' },
    @{ Name = 'hideout_zh.json';   Url = 'https://json.tarkov.dev/regular/hideout_zh' },
    @{ Name = 'hideout_en.json';   Url = 'https://json.tarkov.dev/regular/hideout_en' },
    @{ Name = 'maps.json';         Url = 'https://json.tarkov.dev/regular/maps' },
    @{ Name = 'maps_zh.json';      Url = 'https://json.tarkov.dev/regular/maps_zh' },
    @{ Name = 'maps_en.json';      Url = 'https://json.tarkov.dev/regular/maps_en' },
    @{ Name = 'traders.json';      Url = 'https://json.tarkov.dev/regular/traders' },
    @{ Name = 'traders_zh.json';   Url = 'https://json.tarkov.dev/regular/traders_zh' },
    @{ Name = 'traders_en.json';   Url = 'https://json.tarkov.dev/regular/traders_en' },
    @{ Name = 'endpoints.json';    Url = 'https://json.tarkov.dev/endpoints' }
)

if (-not (Test-Path -LiteralPath $OutputDir)) {
    New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
    Write-Host "Created output dir: $OutputDir"
}

$failCount = 0
$okCount = 0
$skipCount = 0
$totalBytes = 0

foreach ($u in $urls) {
    $target = Join-Path $OutputDir $u.Name

    if ((Test-Path -LiteralPath $target) -and -not $Force) {
        Write-Host "SKIP  $($u.Name) (exists; use -Force to overwrite)"
        $skipCount++
        continue
    }

    Write-Host "FETCH $($u.Name) <- $($u.Url) ..."
    try {
        Invoke-WebRequest -Uri $u.Url -OutFile $target -UseBasicParsing -TimeoutSec 600
        $len = (Get-Item -LiteralPath $target).Length
        $totalBytes += $len
        Write-Host "  OK   $($u.Name) ($len bytes)"
        $okCount++
    } catch {
        Write-Host "  FAIL $($u.Name): $($_.Exception.Message)"
        $failCount++
    }
}

Write-Host ""
Write-Host "=== Summary ==="
Write-Host "OK: $okCount  SKIP: $skipCount  FAIL: $failCount  TotalBytes: $totalBytes"
Write-Host "OutputDir: $OutputDir"

if ($failCount -gt 0) { exit 1 }

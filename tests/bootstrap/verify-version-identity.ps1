# 发版身份契约（C5 · D1/D3）：版本单一源一致性 + 身份禁入扫描。
#
# 第 1 部分：scripts/version/sync-version.mjs 以 check 模式运行，要求 exit 0
#           （即根 package.json 的 version 已传播到全部注册目标，无漂移）。
# 第 2 部分：操作性表面不得残留旧身份。表面 = scripts/**、package.json、
#           .opencode/**、tools/mo2-control-plane/**、tools/*/package.json；
#           模式 = bgs-modding-superpowers | awesome-bgs-mod-master |
#           BGS_MODDING_SUPERPOWERS（大小写不敏感）。
#           说明：BGS 裸词不禁（Bethesda 格式术语与谱系叙述合法）。
#
# 输出风格与同目录既有 bootstrap 脚本一致（PASS/FAIL 行 + 汇总）；exit 0/1。

$ErrorActionPreference = "Stop"

# node 的 UTF-8 输出在 PS 5.1 默认 ANSI 代码页下会乱码；与 verify-all.ps1 保持一致。
try { [Console]::OutputEncoding = [System.Text.Encoding]::UTF8 } catch { }

. (Join-Path $PSScriptRoot "_assert.ps1")

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
$syncScript = Join-Path $repoRoot "scripts/version/sync-version.mjs"

# ---- 1/2 单一源版本一致性 -----------------------------------------------------

Write-Host "  [1/2] version single-source check ..."

if (-not (Test-Path -LiteralPath $syncScript)) {
    Add-BootstrapFailure "missing version sync tool: scripts/version/sync-version.mjs"
} elseif (-not (Get-Command node -ErrorAction SilentlyContinue)) {
    Add-BootstrapFailure "node is not on PATH; scripts/version/sync-version.mjs cannot run"
} else {
    # EAP=Stop 下，node 向 stderr 写字节会触发 NativeCommandError 并中止脚本，
    # 使 drift/配置错误（exit 1/2）无法走到 Add-BootstrapFailure。局部降级为
    # Continue，让 $LASTEXITCODE 决定成败；调用后立即还原。
    $previousErrorActionPreference = $ErrorActionPreference
    $ErrorActionPreference = "Continue"
    try {
        $output = @(& node $syncScript 2>&1)
        $exitCode = $LASTEXITCODE
    } finally {
        $ErrorActionPreference = $previousErrorActionPreference
    }

    foreach ($line in $output) {
        Write-Host ("    {0}" -f $line)
    }

    if ($exitCode -ne 0) {
        Add-BootstrapFailure "version drift detected (node scripts/version/sync-version.mjs exit $exitCode)"
    }
}

# ---- 2/2 身份禁入扫描 ---------------------------------------------------------

Write-Host "  [2/2] release identity scan ..."

$identityPattern = "bgs-modding-superpowers|awesome-bgs-mod-master|BGS_MODDING_SUPERPOWERS"
$identitySurfaces = @(
    "scripts/",
    "package.json",
    ".opencode/",
    "tools/mo2-control-plane/",
    ":(glob)tools/*/package.json"
)

$hits = @(git -C $repoRoot grep -in -E $identityPattern -- $identitySurfaces 2>&1)
$grepExit = $LASTEXITCODE

if ($grepExit -gt 1) {
    Add-BootstrapFailure "git grep identity scan failed (exit $grepExit)"
} elseif ($grepExit -eq 0) {
    foreach ($hit in $hits) {
        $text = [string]$hit
        if ($text -match '^(?<path>[^:]+):(?<line>\d+):') {
            Write-Host ("    HIT {0}:{1}" -f $Matches.path, $Matches.line)
        } else {
            Write-Host ("    HIT {0}" -f $text)
        }
    }
    Add-BootstrapFailure "stale release identity on operational surfaces: $($hits.Count) hit(s)"
}

Complete-BootstrapCheck -SuccessMessage "version single-source consistent; no stale release identity on operational surfaces."

# Modding Standard 检查器夹具回归（C8）
#
# 逐夹具跑 scripts/check-mod-standard.ps1，断言五类事实：
#   1. 进程 exit 码；
#   2. 输出 ID 序列 == registry 中「该 kind 适用」的可检规则发射序列
#      （顺序与集合双断言：覆盖 matrix #7，防止漏跑/重复跑）；
#   3. 汇总行存在、计数和 == 逐条输出条数、FAIL 计数 == 预期 FAIL 集合大小
#      （防重复发射/漏报）；
#   4. FAIL ID 集合精确相等（既不能多也不能少）；
#   5. 关键规则的逐条状态（PASS / FAIL / SKIP / WAIVED）。
#
# 全部夹具通过 exit 0，任一断言失败 exit 1。
#
# 用法：
#   powershell -NoProfile -ExecutionPolicy Bypass -File tests/mod-standard/run-fixtures.ps1

[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"

# 子进程输出按 UTF-8 解码（PS 5.1 缺省用 ANSI 代码页，中文会乱码）
[Console]::OutputEncoding = New-Object System.Text.UTF8Encoding($false)

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
$checker = Join-Path $repoRoot "scripts/check-mod-standard.ps1"
$rulesPath = Join-Path $repoRoot "knowledge/spt-kb/curated/modding-standard/rules.json"
$fixturesRoot = Join-Path $PSScriptRoot "fixtures"

if (-not (Test-Path -LiteralPath $checker)) { Write-Host "checker not found: $checker"; exit 1 }
if (-not (Test-Path -LiteralPath $rulesPath)) { Write-Host "registry not found: $rulesPath"; exit 1 }

$registry = [System.IO.File]::ReadAllText($rulesPath, [System.Text.Encoding]::UTF8) | ConvertFrom-Json

# ---- 期望发射序列（registry 驱动；顺序 = 注册表顺序） --------------------------

function Get-ExpectedEmitIds {
    param([string]$ModKind)

    $ids = New-Object System.Collections.Generic.List[string]
    foreach ($rule in @($registry.rules)) {
        if ($rule.checkable -ne $true) { continue }
        $checkKind = [string]$rule.check.kind
        if ($checkKind -eq "server" -and $ModKind -ne "server") { continue }
        if ($checkKind -eq "client" -and $ModKind -ne "client") { continue }

        if ($null -ne $rule.check.params.emits) {
            foreach ($emitId in @($rule.check.params.emits)) { $ids.Add([string]$emitId) | Out-Null }
        } else {
            $ids.Add([string]$rule.id) | Out-Null
        }
    }
    return $ids
}

# ---- 夹具表 -----------------------------------------------------------------

$fixtures = @(
    @{
        Name         = "pass-server"
        Kind         = "server"
        ExpectExit   = 0
        ExpectFail   = @()
        ExpectStatus = @{
            "STD-STRUCT-005" = "PASS"; "STD-META-005" = "PASS"; "STD-META-003" = "PASS"
            "STD-META-004" = "PASS"; "STD-VER-001" = "PASS"; "STD-VER-002" = "PASS"
            "STD-SRV-002" = "PASS"; "STD-CFG-004" = "PASS"; "STD-CFG-005" = "PASS"
        }
    }
    @{
        Name         = "fail-server"
        Kind         = "server"
        ExpectExit   = 1
        ExpectFail   = @(
            "STD-STRUCT-005", "STD-STRUCT-006", "STD-META-005", "STD-BUILD-001", "STD-BUILD-004",
            "STD-LOG-001", "STD-SRV-001", "STD-SRV-002", "STD-CFG-002", "STD-CFG-005"
        )
        ExpectStatus = @{
            "STD-STRUCT-005" = "FAIL"; "STD-STRUCT-006" = "FAIL"; "STD-META-005" = "FAIL"
            "STD-META-003" = "PASS"; "STD-BUILD-005" = "PASS"; "STD-BUILD-006" = "PASS"
            "STD-VER-001" = "PASS"; "STD-VER-002" = "PASS"; "STD-SRV-003" = "PASS"
            "STD-SRV-008" = "PASS"; "STD-CFG-003" = "PASS"; "STD-CFG-004" = "PASS"
        }
    }
    @{
        Name         = "pass-client"
        Kind         = "client"
        ExpectExit   = 0
        ExpectFail   = @()
        ExpectStatus = @{
            "STD-META-005" = "PASS"; "STD-BUILD-002" = "PASS"; "STD-CLI-001" = "PASS"
            "STD-META-006" = "PASS"; "STD-CLI-003" = "PASS"; "STD-CLI-007" = "PASS"
            "STD-CLI-006" = "PASS"; "STD-CFG-006" = "PASS"
        }
    }
    @{
        Name         = "placeholder-server"
        Kind         = "server"
        ExpectExit   = 0
        ExpectFail   = @()
        ExpectStatus = @{
            "STD-META-005" = "SKIP"; "STD-META-003" = "SKIP"; "STD-VER-002" = "SKIP"
            "STD-META-004" = "PASS"; "STD-VER-001" = "PASS"; "STD-STRUCT-005" = "PASS"
        }
    }
    @{
        Name         = "waiver-mod"
        Kind         = "server"
        ExpectExit   = 0
        ExpectFail   = @()
        ExpectStatus = @{
            "STD-STRUCT-006" = "WAIVED"; "STD-STRUCT-005" = "PASS"; "STD-META-005" = "PASS"
        }
    }
    @{
        Name         = "meta005-prerelease-client"
        Kind         = "client"
        ExpectExit   = 0
        ExpectFail   = @()
        ExpectStatus = @{
            "STD-META-005" = "PASS"; "STD-CLI-001" = "PASS"
        }
    }
    @{
        Name         = "meta005-fourpart-client"
        Kind         = "client"
        ExpectExit   = 1
        ExpectFail   = @("STD-META-005")
        ExpectStatus = @{
            "STD-META-005" = "FAIL"; "STD-CLI-001" = "PASS"
        }
    }
)

# ---- 检查器调用 -------------------------------------------------------------

$script:markerToStatus = @{ PASS = "PASS"; FAIL = "FAIL"; SKIP = "SKIP"; WAIV = "WAIVED" }

function Invoke-FixtureChecker {
    param([string]$ModPath, [string]$ModKind)

    $command = "[Console]::OutputEncoding = New-Object System.Text.UTF8Encoding(`$false); " +
               "& '$checker' -ModPath '$ModPath' -Kind $ModKind -TargetSptVersion '4.1.5'; exit `$LASTEXITCODE"

    $raw = & powershell -NoProfile -ExecutionPolicy Bypass -Command $command 2>&1
    $exitCode = $LASTEXITCODE
    $lines = @($raw | ForEach-Object { [string]$_ })

    $ids = New-Object System.Collections.Generic.List[string]
    $statusOf = @{}
    foreach ($line in $lines) {
        $m = [regex]::Match($line, '^\[(PASS|FAIL|SKIP|WAIV)\]\s+(STD-[A-Z]+-\d{3})')
        if ($m.Success) {
            $id = $m.Groups[2].Value
            $ids.Add($id) | Out-Null
            $statusOf[$id] = $script:markerToStatus[$m.Groups[1].Value]
        }
    }

    $counts = @{}
    $summary = [regex]::Match(($lines -join "`n"), 'checks: PASS=(\d+) FAIL=(\d+) SKIP=(\d+) WAIVED=(\d+)')
    if ($summary.Success) {
        $counts = @{
            PASS = [int]$summary.Groups[1].Value; FAIL = [int]$summary.Groups[2].Value
            SKIP = [int]$summary.Groups[3].Value; WAIVED = [int]$summary.Groups[4].Value
        }
    }

    return @{ Exit = $exitCode; Lines = $lines; Ids = $ids; StatusOf = $statusOf; Counts = $counts; HasSummary = $summary.Success }
}

# ---- 主循环 -----------------------------------------------------------------

$script:failures = New-Object System.Collections.Generic.List[string]
$script:passed = 0

foreach ($fixture in $fixtures) {
    $name = [string]$fixture.Name
    $modPath = Join-Path $fixturesRoot $name
    $problems = New-Object System.Collections.Generic.List[string]

    if (-not (Test-Path -LiteralPath $modPath)) {
        $problems.Add("fixture directory not found: $modPath")
    } else {
        $result = Invoke-FixtureChecker -ModPath $modPath -ModKind ([string]$fixture.Kind)

        # 1. exit 码
        if ($result.Exit -ne [int]$fixture.ExpectExit) {
            $problems.Add("exit code: expected $($fixture.ExpectExit), got $($result.Exit)")
        }

        # 2. 发射序列 == registry 期望序列（顺序 + 集合）
        $expectedIds = Get-ExpectedEmitIds -ModKind ([string]$fixture.Kind)
        $observedIds = @($result.Ids)
        if ($observedIds.Count -ne $expectedIds.Count) {
            $problems.Add("emitted id count: expected $($expectedIds.Count), got $($observedIds.Count)")
        }
        $maxCount = [Math]::Max($observedIds.Count, $expectedIds.Count)
        for ($i = 0; $i -lt $maxCount; $i++) {
            $want = if ($i -lt $expectedIds.Count) { $expectedIds[$i] } else { "<none>" }
            $got = if ($i -lt $observedIds.Count) { $observedIds[$i] } else { "<none>" }
            if ($want -ne $got) {
                $problems.Add("emitted id order at position $($i + 1): expected '$want', got '$got'")
                break
            }
        }

        # 3. 汇总行与逐条计数一致（防止重复发射）
        if (-not $result.HasSummary) {
            $problems.Add("summary line 'checks: PASS=.. FAIL=.. SKIP=.. WAIVED=..' not found")
        } else {
            $sum = $result.Counts.PASS + $result.Counts.FAIL + $result.Counts.SKIP + $result.Counts.WAIVED
            if ($sum -ne $observedIds.Count) {
                $problems.Add("summary counters sum to $sum but $($observedIds.Count) status line(s) were emitted")
            }
            if ($result.Counts.FAIL -ne @($fixture.ExpectFail).Count) {
                $problems.Add("summary FAIL count: expected $(@($fixture.ExpectFail).Count), got $($result.Counts.FAIL)")
            }
        }

        # 4. FAIL ID 集合精确相等
        $observedFail = @($result.Ids | Where-Object { $result.StatusOf[$_] -eq "FAIL" } | Sort-Object -Unique)
        $expectedFail = @(@($fixture.ExpectFail) | Sort-Object -Unique)
        $missing = @($expectedFail | Where-Object { $observedFail -notcontains $_ })
        $extra = @($observedFail | Where-Object { $expectedFail -notcontains $_ })
        if ($missing.Count -gt 0) { $problems.Add("missing expected FAIL: $($missing -join ', ')") }
        if ($extra.Count -gt 0) { $problems.Add("unexpected FAIL: $($extra -join ', ')") }

        # 5. 关键规则逐条状态
        foreach ($id in @($fixture.ExpectStatus.Keys)) {
            $want = [string]$fixture.ExpectStatus[$id]
            if (-not $result.StatusOf.ContainsKey($id)) {
                $problems.Add("$id : expected $want, but the rule was not emitted")
            } elseif ($result.StatusOf[$id] -ne $want) {
                $problems.Add("$id : expected $want, got $($result.StatusOf[$id])")
            }
        }
    }

    if ($problems.Count -eq 0) {
        $script:passed++
        Write-Host ("  [PASS] {0} (kind={1}, exit={2})" -f $name, $fixture.Kind, $fixture.ExpectExit)
    } else {
        Write-Host ("  [FAIL] {0} (kind={1})" -f $name, $fixture.Kind)
        foreach ($p in $problems) { Write-Host ("         - {0}" -f $p) }
        foreach ($p in $problems) { $script:failures.Add("$name : $p") | Out-Null }
    }
}

Write-Host ""
Write-Host ("[run-fixtures] {0}/{1} fixture(s) passed" -f $script:passed, $fixtures.Count)

if ($script:failures.Count -gt 0) {
    Write-Host ("[run-fixtures] FAIL：$($script:failures.Count) 条断言未满足")
    exit 1
}

Write-Host "[run-fixtures] OK：全部夹具断言通过"
exit 0

# Modding Standard 校验器「维度数数据驱动」回归（ticket 01）
#
# 证明 scripts/validate-mod-standard.ps1 的维度数来自 registry.domains，而非硬编码：
#   1. 复制标准目录到临时夹具（.artifacts/mod-standard-validator-test/，已 gitignore）；
#      加一个 DUMMY domain + 14-dummy.md + 对应 registry 规则条目；
#      校验器无需改代码即接受 14 个维度（exit 0）。
#   2. 从复制品的 rules.json 移除 DUMMY domain、保留 14-dummy.md 章节；
#      校验器检出维度数失配（exit 1）。
#
# rules.json 的改动走对象序列化（ConvertFrom-Json -> Add-Member -> ConvertTo-Json），
# 不做脆弱的字符串拼接。临时目录的清理包在 try/finally 中，任何提前失败/异常都会清理。
#
# 用法：
#   powershell -NoProfile -ExecutionPolicy Bypass -File tests/mod-standard/validator-dimension-count.ps1

[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"

# 子进程输出按 UTF-8 解码（PS 5.1 缺省用 ANSI 代码页，中文会乱码）
[Console]::OutputEncoding = New-Object System.Text.UTF8Encoding($false)

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
$validator = Join-Path $repoRoot "scripts/validate-mod-standard.ps1"
$srcStdDir = Join-Path $repoRoot "knowledge/spt-kb/curated/modding-standard"
$workRoot = Join-Path $repoRoot ".artifacts/mod-standard-validator-test"
$tempStd = Join-Path $workRoot "standard"

$script:passed = 0
$script:failures = New-Object System.Collections.Generic.List[string]
$exitCode = 0

function Assert-True {
    param([bool]$Condition, [string]$Message)
    if ($Condition) {
        $script:passed++
        Write-Host "  [PASS] $Message"
    } else {
        $script:failures.Add($Message) | Out-Null
        Write-Host "  [FAIL] $Message"
    }
}

function Invoke-Validator {
    param([string]$StdDir)
    $raw = & powershell -NoProfile -ExecutionPolicy Bypass -File $validator -StdDir $StdDir 2>&1
    return @{ Exit = $LASTEXITCODE; Text = (@($raw | ForEach-Object { [string]$_ }) -join "`n") }
}

function Write-Utf8NoBom {
    param([string]$Path, [string]$Text)
    [System.IO.File]::WriteAllText($Path, $Text, (New-Object System.Text.UTF8Encoding($false)))
}

try {
    if (-not (Test-Path -LiteralPath $validator -PathType Leaf)) {
        $script:failures.Add("validator not found: $validator") | Out-Null
    }
    if (-not (Test-Path -LiteralPath $srcStdDir -PathType Container)) {
        $script:failures.Add("standard dir not found: $srcStdDir") | Out-Null
    }

    if ($script:failures.Count -eq 0) {

        # ---- 干净夹具 -------------------------------------------------------

        if (Test-Path -LiteralPath $workRoot) { Remove-Item -LiteralPath $workRoot -Recurse -Force }
        New-Item -ItemType Directory -Path $workRoot | Out-Null
        Copy-Item -LiteralPath $srcStdDir -Destination $tempStd -Recurse

        $tempRules = Join-Path $tempStd "rules.json"
        $tempChapter = Join-Path $tempStd "14-dummy.md"

        # ---- 1. 加 DUMMY domain + 章节 + registry 条目（对象序列化） --------

        $rules = [System.IO.File]::ReadAllText($tempRules, [System.Text.Encoding]::UTF8) | ConvertFrom-Json
        if ($null -eq $rules.domains) { throw "copied rules.json missing domains" }

        $rules.domains | Add-Member -NotePropertyName "DUMMY" -NotePropertyValue "14-dummy.md"

        $dummyRule = [pscustomobject]@{
            id        = "STD-DUMMY-001"
            domain    = "DUMMY"
            level     = "SHOULD"
            applies   = "both"
            title     = "Dummy rule"
            checkable = $false
            check     = $null
        }
        $rules.rules = @($dummyRule) + @($rules.rules)

        Write-Utf8NoBom -Path $tempRules -Text ($rules | ConvertTo-Json -Depth 100)

        $emDash = [char]0x2014
        $chapterText = @"
# 14 Dummy（DUMMY）

> **Domain slug:** ``DUMMY`` $emDash **规则 ID 前缀:** ``STD-DUMMY-``

## 规则

### STD-DUMMY-001 $emDash Dummy rule

- **Level:** SHOULD
- **Applies:** both
- **Evidence:** 机制：test
"@
        Write-Utf8NoBom -Path $tempChapter -Text $chapterText

        Write-Host "[validator-dimension-count] 夹具：14 维度（DUMMY 已加入 registry.domains）"
        $run1 = Invoke-Validator -StdDir $tempStd
        Assert-True ($run1.Exit -eq 0) "14 维度被接受（期望 exit 0，实际 $($run1.Exit)）"
        if ($run1.Exit -ne 0) { Write-Host $run1.Text }

        # ---- 2. 移除 DUMMY domain、保留章节 -> 失配 -------------------------

        $rules.domains.PSObject.Properties.Remove("DUMMY")
        Write-Utf8NoBom -Path $tempRules -Text ($rules | ConvertTo-Json -Depth 100)

        Write-Host "[validator-dimension-count] 夹具：DUMMY domain 已移除，14-dummy.md 保留（13 维度声明 vs 14 章节）"
        $run2 = Invoke-Validator -StdDir $tempStd
        Assert-True ($run2.Exit -eq 1) "维度数失配被检出（期望 exit 1，实际 $($run2.Exit)）"
        Assert-True ($run2.Text.Contains("维度章节数应为 13")) "错误信息含期望计数 13"
        Assert-True ($run2.Text.Contains("实际：14")) "错误信息含实际计数 14"
        if ($run2.Exit -ne 1) { Write-Host $run2.Text }
    }
}
finally {
    # 无论成功、提前失败还是异常，都清理临时夹具目录。
    if (Test-Path -LiteralPath $workRoot) { Remove-Item -LiteralPath $workRoot -Recurse -Force }
}

Write-Host ""
Write-Host ("[validator-dimension-count] {0} 断言通过，{1} 失败" -f $script:passed, $script:failures.Count)

if ($script:failures.Count -gt 0) {
    Write-Host "[validator-dimension-count] FAIL"
    foreach ($f in $script:failures) { Write-Host "  - $f" }
    $exitCode = 1
} else {
    Write-Host "[validator-dimension-count] OK：维度数数据驱动已验证"
}

exit $exitCode

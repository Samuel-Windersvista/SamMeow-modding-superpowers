# Modding Standard 注册表校验器（C8）
#
# 双向校验 rules.json（机读注册表）↔ 13 章 prose（唯一权威文本）：
#   1. ID 集合一致（prose `### STD-...` ↔ registry；无孤儿、无缺失）；
#   2. ID 唯一性两侧各查：registry 内不重复；prose 内同一 ID 出现两次（重复标题）报错；
#   3. 每条规则的 title / Level / Applies 与 prose 逐字一致。Level / Applies /
#      Evidence 按**规则块作用域**解析（本规则标题 -> 下一条 `###` 标题或文末），
#      缺行不得继承下一条的值；
#   4. domain slug 合法且与该规则所在章节一致；维度章节数 = 13；
#   5. registry 内部：ID 格式、checkable 必有 check 规格、非 checkable 的 check
#      必须为 null、check.kind/handler/params 齐备；
#   6. 声明式 handler 的必需 params 键齐备（键名拼错 = 缺键；否则取值类检查会静默
#      退化为恒 PASS）；
#   7. MUST 规则的 Evidence 行须含「机制」与「语料」双源标记（README「分级与证据
#      标准」「无语料先例」；无语料先例的规则按单源判 SHOULD，故 MUST 不应带该标记）；
#   8. 发射 ID（check.params.emits，缺省为规则自身 ID）必须落在 registry 内，或属
#      显式登记的「检查器专用」集合；同一 ID 不得被跨规则重复发射；白名单不得出现
#      死条目（登记了却没有任何检查发射它）。
#
# 说明：prose 扫描范围仅 13 个维度章节（`NN-*.md`）；README / evidence-index /
# version-matrix 不参与（README 含示例行，不是规则来源）。
#
# 退出码：0 = 双向一致；1 = 存在校验错误。
#
# 用法:
#   powershell -NoProfile -ExecutionPolicy Bypass -File scripts/validate-mod-standard.ps1

[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"

# 控制台输出按 UTF-8 写出（PS 5.1 默认用 ANSI 代码页，中文会乱码）。
# 用不带 BOM 的 UTF8Encoding：带 BOM 的 [System.Text.Encoding]::UTF8 在重定向/
# 管道场景会把前导字节混进流里，被下游按自身编码误解码。
try { [Console]::OutputEncoding = New-Object System.Text.UTF8Encoding($false) } catch { }

$repoRoot = Split-Path -Parent $PSScriptRoot
$stdDir = Join-Path $repoRoot "knowledge/spt-kb/curated/modding-standard"
$rulesPath = Join-Path $stdDir "rules.json"

# 检查器专用发射 ID（prose 无对应规则）——显式登记；出现新 ID 即报错。
# 决策记录：STD-VER-001 是 STD-META-004（sptversion-range）的第二行输出，旧检查器
# 时期即存在；本轮维持登记、不静默对齐 prose，理由见 C8 dev-log 台账。
$CheckerOnlyEmitIds = @("STD-VER-001")

# 声明式 handler -> 必需 params 键。缺键（含键名拼错）即报错：取值类 handler 若读不到
# 参数会静默退化为恒 PASS（例如 no-files-by-extension 的 extensions 拼错 -> 恒 PASS）。
# 命名 handler（meta-003-guid / sptversion-range / ver-002-version-single-source /
# meta-005-version-semver / cli-001-plugin-base / cfg-004-injectable-config /
# cli-007-harmony-lifecycle）的逻辑内嵌，键集随实现走，不在本表约束。
$HandlerRequiredKeys = @{
    "no-files-by-extension"  = @("extensions", "messages")
    "repo-file-exists"       = @("names")
    "mod-file-exists"        = @("names")
    "proj-value-equals"      = @("name", "value", "messages")
    "proj-value-in"          = @("name", "allowed", "messages")
    "proj-regex-present"     = @("patterns", "sources")
    "proj-refs-present"      = @("refs", "messages")
    "src-regex-present"      = @("pattern")
    "src-regex-absent"       = @("pattern")
    "src-regex-count-equals" = @("pattern", "count", "messages")
    "meta-file-contains"     = @("fileName", "pattern")
}

$errors = New-Object System.Collections.Generic.List[string]
$deviations = New-Object System.Collections.Generic.List[string]

function Add-ValidationError {
    param([string]$Message)
    $errors.Add($Message) | Out-Null
}

if (-not (Test-Path -LiteralPath $rulesPath)) {
    Write-Host "[validate-mod-standard] 缺少注册表：$rulesPath"
    exit 1
}

$registry = [System.IO.File]::ReadAllText($rulesPath, [System.Text.Encoding]::UTF8) | ConvertFrom-Json

# ---- registry 内部规则 ------------------------------------------------------

if ($registry.schema_version -ne 1) {
    Add-ValidationError "schema_version 必须为 1（实际：$($registry.schema_version)）"
}
if ($null -eq $registry.rules) {
    Add-ValidationError "registry 缺少 rules 数组"
}
if ($null -eq $registry.domains) {
    Add-ValidationError "registry 缺少 domains 映射"
}

$rules = @($registry.rules)
$seenIds = @{}
$emittedIds = New-Object System.Collections.Generic.List[string]

foreach ($rule in $rules) {
    $id = [string]$rule.id
    if ($id -notmatch '^STD-[A-Z]+-\d{3}$') {
        Add-ValidationError "非法规则 ID：'$id'"
        continue
    }
    if ($seenIds.ContainsKey($id)) {
        Add-ValidationError "规则 ID 重复：$id"
    } else {
        $seenIds[$id] = $true
    }

    if ($null -eq $registry.domains.PSObject.Properties[$rule.domain]) {
        Add-ValidationError "$id：domain '$($rule.domain)' 不在 domains 映射中"
    }

    if ($rule.checkable -eq $true) {
        $check = $rule.check
        if ($null -eq $check) {
            Add-ValidationError "$id：checkable=true 但 check 为 null"
            continue
        }
        if (@("both", "server", "client") -notcontains [string]$check.kind) {
            Add-ValidationError "$id：check.kind 必须为 both/server/client（实际：$($check.kind)）"
        }
        if ([string]::IsNullOrWhiteSpace([string]$check.handler)) {
            Add-ValidationError "$id：check.handler 不能为空"
        }
        if ($null -eq $check.params) {
            Add-ValidationError "$id：check.params 不能为 null"
        } elseif ($HandlerRequiredKeys.ContainsKey([string]$check.handler)) {
            foreach ($requiredKey in $HandlerRequiredKeys[[string]$check.handler]) {
                if ($null -eq $check.params.PSObject.Properties[$requiredKey]) {
                    Add-ValidationError "$id：handler '$($check.handler)' 缺必需 params 键 '$requiredKey'（漏写或键名拼错）"
                }
            }
        }

        # 发射 ID：缺省为规则自身；params.emits 可声明多条（本检查发射多行输出）
        $emits = @($id)
        if ($null -ne $check.params -and $null -ne $check.params.emits) {
            $emits = @($check.params.emits)
        }
        foreach ($emitId in $emits) {
            $emittedIds.Add([string]$emitId) | Out-Null
        }
    } else {
        if ($null -ne $rule.check) {
            Add-ValidationError "$id：checkable=false 但 check 非 null"
        }
    }
}

# 发射 ID 归属：registry 内 ID 或显式登记的检查器专用 ID；同一 ID 不得被重复发射
$emitSeen = @{}
foreach ($emitId in $emittedIds) {
    if ($emitSeen.ContainsKey($emitId)) {
        Add-ValidationError "发射 ID 被重复发射：$emitId（检查器会对同一 ID 输出两行，破坏汇总计数与 golden）"
    } else {
        $emitSeen[$emitId] = $true
    }

    if (-not $seenIds.ContainsKey($emitId)) {
        if ($CheckerOnlyEmitIds -contains $emitId) {
            $deviations.Add("检查器专用发射 ID（prose 无对应规则）：$emitId") | Out-Null
        } else {
            Add-ValidationError "未登记的发射 ID：$emitId（既不在 registry，也不在检查器专用集合）"
        }
    }
}

# 白名单死条目：登记了却没有任何检查发射它（改名/删除规则后残留的登记）
foreach ($onlyId in $CheckerOnlyEmitIds) {
    if (-not $emitSeen.ContainsKey($onlyId)) {
        Add-ValidationError "检查器专用发射 ID 白名单死条目：$onlyId（没有任何可检规则发射它）"
    }
}

# ---- prose 解析 -------------------------------------------------------------

$proseRules = New-Object System.Collections.Generic.List[object]
$proseDomains = @{}
$chapters = @(Get-ChildItem -LiteralPath $stdDir -File -Filter "*.md" |
    Where-Object { $_.Name -match '^\d{2}-' } | Sort-Object Name)

if ($chapters.Count -ne 13) {
    Add-ValidationError "维度章节数应为 13（实际：$($chapters.Count)）"
}

$seenProseIds = @{}

foreach ($chapter in $chapters) {
    $text = [System.IO.File]::ReadAllText($chapter.FullName, [System.Text.Encoding]::UTF8)

    $slugMatch = [regex]::Match($text, 'Domain slug:\*\*\s*`([A-Z]+)`')
    if (-not $slugMatch.Success) {
        Add-ValidationError "$($chapter.Name)：未找到 Domain slug"
        continue
    }
    $slug = $slugMatch.Groups[1].Value
    $proseDomains[$slug] = $chapter.Name

    # 标题分隔符为 em dash（U+2014）；用 \u2014 转义避免脚本编码敏感性
    $headMatches = @([regex]::Matches($text, '(?m)^###\s+(STD-[A-Z]+-\d{3})\s+\u2014\s+(.+?)\s*$'))
    for ($i = 0; $i -lt $headMatches.Count; $i++) {
        $head = $headMatches[$i]
        $id = $head.Groups[1].Value

        # 块级作用域：本规则标题 -> 下一条 `###` 标题（或文末）。
        # 旧实现取 Substring($head.Index) 到 EOF，缺 Level/Applies/Evidence 行时会静默
        # 继承下一条规则的值，使「逐字一致」断言在数据缺行时仍然通过。
        $blockEnd = if ($i + 1 -lt $headMatches.Count) { $headMatches[$i + 1].Index } else { $text.Length }
        $block = $text.Substring($head.Index, $blockEnd - $head.Index)

        # prose 侧 ID 唯一性：重复标题会让 $proseById 覆盖式合并、静默吞掉一条规则
        if ($seenProseIds.ContainsKey($id)) {
            Add-ValidationError "prose 规则 ID 重复：$id（@ $($chapter.Name)；首次出现 @ $($seenProseIds[$id])）"
        } else {
            $seenProseIds[$id] = $chapter.Name
        }

        $level = [regex]::Match($block, '(?m)^-\s+\*\*Level:\*\*\s*(.+?)\s*$').Groups[1].Value
        $applies = [regex]::Match($block, '(?m)^-\s+\*\*Applies:\*\*\s*(.+?)\s*$').Groups[1].Value
        $evidence = [regex]::Match($block, '(?m)^-\s+\*\*Evidence:\*\*\s*(.+?)\s*$').Groups[1].Value

        $proseRules.Add([pscustomobject]@{
            Id       = $id
            Domain   = $slug
            Level    = $level
            Applies  = $applies
            Evidence = $evidence
            Title    = $head.Groups[2].Value
            Chapter  = $chapter.Name
        }) | Out-Null
    }
}

# domains 映射 ↔ prose 实际 slug
foreach ($slug in $proseDomains.Keys) {
    if ($null -eq $registry.domains.PSObject.Properties[$slug]) {
        Add-ValidationError "prose domain '$slug' 不在 registry.domains 中"
    }
}
foreach ($prop in $registry.domains.PSObject.Properties) {
    if (-not $proseDomains.ContainsKey($prop.Name)) {
        Add-ValidationError "registry.domains 含未知 domain '$($prop.Name)'"
    }
}

# ---- MUST 双源证据标记 ------------------------------------------------------
# README「分级与证据标准」：MUST 最低证据要求为机制 + 语料双源；「无语料先例」的规则
# 按单源判 SHOULD，因此 MUST 的 Evidence 行必须同时出现「机制」与「语料」标记。
# 注：「无语料先例」本身含「语料」子串，故单检「语料」即覆盖两种写法。

$mustTotal = 0
$mustWithMarkers = 0
foreach ($p in $proseRules) {
    if ([string]$p.Level -cne 'MUST') { continue }
    $mustTotal++
    $evidence = [string]$p.Evidence
    $hasMechanism = $evidence.Contains('机制')
    $hasCorpus = $evidence.Contains('语料')
    if ($hasMechanism -and $hasCorpus) {
        $mustWithMarkers++
    } else {
        if (-not $hasMechanism) {
            Add-ValidationError "$($p.Id)：MUST 规则 Evidence 缺「机制」证据标记（@ $($p.Chapter)）"
        }
        if (-not $hasCorpus) {
            Add-ValidationError "$($p.Id)：MUST 规则 Evidence 缺「语料」证据标记（@ $($p.Chapter)）"
        }
    }
}

# ---- 双向比对 ---------------------------------------------------------------

$proseById = @{}
foreach ($p in $proseRules) { $proseById[$p.Id] = $p }

foreach ($p in $proseRules) {
    if (-not $seenIds.ContainsKey($p.Id)) {
        Add-ValidationError "prose 规则未登记进 registry：$($p.Id)"
    }
}
foreach ($rule in $rules) {
    $id = [string]$rule.id
    if (-not $proseById.ContainsKey($id)) {
        Add-ValidationError "registry 规则在 prose 中不存在：$id"
        continue
    }
    $p = $proseById[$id]
    if ([string]$rule.title -cne [string]$p.Title) {
        Add-ValidationError "$id：title 与 prose 不一致`n    registry: '$($rule.title)'`n    prose   : '$($p.Title)'"
    }
    if ([string]$rule.level -cne [string]$p.Level) {
        Add-ValidationError "$id：level 与 prose 不一致（registry='$($rule.level)' prose='$($p.Level)'）"
    }
    if ([string]$rule.applies -cne [string]$p.Applies) {
        Add-ValidationError "$id：applies 与 prose 不一致（registry='$($rule.applies)' prose='$($p.Applies)'）"
    }
    if ([string]$rule.domain -cne [string]$p.Domain) {
        Add-ValidationError "$id：domain 与所在章节不一致（registry='$($rule.domain)' chapter='$($p.Domain)' @ $($p.Chapter)）"
    }
}

# ---- 统计与输出 -------------------------------------------------------------

$byLevel = @{ MUST = 0; SHOULD = 0; MAY = 0 }
foreach ($rule in $rules) {
    $lvl = [string]$rule.level
    if ($byLevel.ContainsKey($lvl)) { $byLevel[$lvl]++ } else { $byLevel[$lvl] = 1 }
}
$checkable = @($rules | Where-Object { $_.checkable -eq $true }).Count

Write-Host "[validate-mod-standard] registry : $rulesPath"
Write-Host "[validate-mod-standard] prose    : $($chapters.Count) 章 / $($proseRules.Count) 条"
Write-Host ("[validate-mod-standard] 统计     : {0} 条 / MUST={1} SHOULD={2} MAY={3} / 可检={4} / 发射 ID={5}" -f `
    $rules.Count, $byLevel["MUST"], $byLevel["SHOULD"], $byLevel["MAY"], $checkable, $emittedIds.Count)
Write-Host ("[validate-mod-standard] 证据     : MUST 双源标记 {0}/{1}（机制 + 语料）" -f $mustWithMarkers, $mustTotal)

foreach ($d in $deviations) {
    Write-Host "  [登记] $d"
}

if ($errors.Count -gt 0) {
    Write-Host ""
    Write-Host "[validate-mod-standard] FAIL：$($errors.Count) 个校验错误"
    foreach ($e in $errors) { Write-Host "  - $e" }
    exit 1
}

Write-Host "[validate-mod-standard] OK：registry ↔ prose 双向一致"
exit 0

# Modding Standard 机械检查器（registry 驱动）
#
# 读取 knowledge/spt-kb/curated/modding-standard/rules.json（机读注册表，单一源），
# 按注册表顺序对一个 mod 源码目录执行「可检子集」检查；每条规则报
# PASS / FAIL / SKIP / WAIVED。存在未豁免 FAIL 时 exit 1。
#
# 稳定接口（供后续工具依赖）：
#   - 规则 ID 即锚点：STD-<DOMAIN>-<NNN>；顺序 = 注册表顺序。
#   - 规则常量（正则 / 取值 / 消息模板）全部落在 rules.json 的 check.params；
#     handler 只负责取数与判定的通用机制，不内嵌任何规则常量。
#   - 声明式 handler 覆盖简单检查（no-files-by-extension / repo-file-exists /
#     proj-value-regex 家族 / src-regex 家族 / …）；复杂逻辑保留为命名 handler
#     （meta-005-version-semver、sptversion-range、ver-002-version-single-source、
#      cfg-004-injectable-config、cli-001-plugin-base、cli-007-harmony-lifecycle）。
#   - 脚手架占位符 {{LIKE_THIS}} 把「取值形状」类检查降级为 SKIP
#     （先实例化模板，再重跑）。
#   - 豁免：豁免文件（缺省 <mod>/MODDING-STD-WAIVER.md）中的
#       Waiver: STD-XXX-NNN: <reason>
#     行把 FAIL 降级为 WAIVED。
#
# 用法：
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

# 控制台输出按 UTF-8 写出（PS 5.1 默认用 ANSI 代码页，中文会乱码）。
# 用不带 BOM 的 UTF8Encoding：带 BOM 的 [System.Text.Encoding]::UTF8 在重定向/
# 管道场景会把前导字节混进流里，被下游按自身编码误解码。
try { [Console]::OutputEncoding = New-Object System.Text.UTF8Encoding($false) } catch { }

$modRoot = (Resolve-Path -LiteralPath $ModPath).Path
if (-not (Test-Path -LiteralPath $modRoot -PathType Container)) {
    Write-Error "ModPath is not a directory: $ModPath"
}

# ---- registry 加载（脚本相对路径 -> 仓库根；便携树同布局） --------------------

$repoRoot = Split-Path -Parent $PSScriptRoot
$rulesPath = Join-Path $repoRoot "knowledge/spt-kb/curated/modding-standard/rules.json"
if (-not (Test-Path -LiteralPath $rulesPath)) {
    Write-Error "Modding Standard registry not found: $rulesPath"
}
$registry = [System.IO.File]::ReadAllText($rulesPath, [System.Text.Encoding]::UTF8) | ConvertFrom-Json

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

# 消息模板填充：{name} -> 值（模板文本本身来自 rules.json 的 check.params.messages）
function Format-Message {
    param([string]$Template, [hashtable]$Values)
    if ([string]::IsNullOrEmpty($Template)) { return "" }
    $out = $Template
    foreach ($key in $Values.Keys) {
        $out = $out.Replace("{" + $key + "}", [string]$Values[$key])
    }
    return $out
}

function New-Result {
    param([string]$Id, [bool]$Ok, [string]$Detail = "", [string]$SkipReason = "")
    return @{ Id = $Id; Ok = $Ok; Detail = $Detail; SkipReason = $SkipReason }
}

# handler 共享上下文（原检查器的全局取数，语义不变）
$metaFile = $csFiles | Where-Object { $_.Name -eq "ModMetadata.cs" } | Select-Object -First 1
$meta = if ($metaFile) { [System.IO.File]::ReadAllText($metaFile.FullName, [System.Text.Encoding]::UTF8) } else { "" }
$projVer = Get-ProjValue "Version"

# ---- handlers（声明式 + 命名；常量全部来自 check.params） --------------------

$script:handlers = @{}

# 文件集合：禁止出现指定扩展名的文件（detail 恒输出）
$script:handlers["no-files-by-extension"] = {
    param($check, $emitIds)
    $exts = @($check.params.extensions)
    $hits = @($files | Where-Object { $exts -contains $_.Extension })
    $detail = Format-Message $check.params.messages.detail @{ count = $hits.Count }
    return (New-Result $emitIds[0] ($hits.Count -eq 0) $detail)
}

# 仓库文件存在（mod 目录或 monorepo 根，任一命中即可）
$script:handlers["repo-file-exists"] = {
    param($check, $emitIds)
    $ok = $false
    foreach ($name in @($check.params.names)) {
        if (Test-RepoFile $name) { $ok = $true; break }
    }
    return (New-Result $emitIds[0] $ok)
}

# mod 目录内文件存在（全部命中才算 PASS）
$script:handlers["mod-file-exists"] = {
    param($check, $emitIds)
    $ok = $true
    foreach ($name in @($check.params.names)) {
        if (-not (Test-Path -LiteralPath (Join-Path $modRoot $name))) { $ok = $false }
    }
    return (New-Result $emitIds[0] $ok)
}

# csproj/props 取值等于常量
$script:handlers["proj-value-equals"] = {
    param($check, $emitIds)
    $p = $check.params
    $pv = Get-ProjValue $p.name
    $value = if ($null -ne $pv) { [string]$pv.Value } else { "" }
    $source = if ($null -ne $pv) { [string]$pv.Source } else { "" }
    $detail = Format-Message $p.messages.detail @{ value = $value; source = $source }
    return (New-Result $emitIds[0] ($value -eq [string]$p.value) $detail)
}

# csproj/props 取值属于白名单
$script:handlers["proj-value-in"] = {
    param($check, $emitIds)
    $p = $check.params
    $pv = Get-ProjValue $p.name
    $value = if ($null -ne $pv) { [string]$pv.Value } else { "" }
    $source = if ($null -ne $pv) { [string]$pv.Source } else { "" }
    $detail = Format-Message $p.messages.detail @{ value = $value; source = $source }
    return (New-Result $emitIds[0] (@($p.allowed) -contains $value) $detail)
}

# 指定文本源（proj / props）中出现任一模式
$script:handlers["proj-regex-present"] = {
    param($check, $emitIds)
    $p = $check.params
    $texts = @()
    foreach ($sourceName in @($p.sources)) {
        if ($sourceName -eq "proj") { $texts += $proj }
        elseif ($sourceName -eq "props") { $texts += $propsText }
    }
    $ok = $false
    foreach ($pattern in @($p.patterns)) {
        foreach ($text in $texts) { if ($text -match $pattern) { $ok = $true; break } }
        if ($ok) { break }
    }
    return (New-Result $emitIds[0] $ok)
}

# csproj 引用集合齐备
$script:handlers["proj-refs-present"] = {
    param($check, $emitIds)
    $p = $check.params
    $missing = @($p.refs | Where-Object { $proj -notmatch [regex]::Escape($_) })
    $detail = if ($missing.Count -gt 0) {
        Format-Message $p.messages.missing @{ list = ($missing -join ",") }
    } else {
        [string]$p.messages.ok
    }
    return (New-Result $emitIds[0] ($missing.Count -eq 0) $detail)
}

# C# 源码中出现模式
$script:handlers["src-regex-present"] = {
    param($check, $emitIds)
    return (New-Result $emitIds[0] ($src -match $check.params.pattern))
}

# C# 源码中不得出现模式
$script:handlers["src-regex-absent"] = {
    param($check, $emitIds)
    return (New-Result $emitIds[0] ($src -notmatch $check.params.pattern))
}

# C# 源码中模式出现次数等于常量
$script:handlers["src-regex-count-equals"] = {
    param($check, $emitIds)
    $p = $check.params
    $count = [regex]::Matches($src, $p.pattern).Count
    $detail = Format-Message $p.messages.detail @{ count = $count }
    return (New-Result $emitIds[0] ($count -eq [int]$p.count) $detail)
}

# 指定文件名的 .cs 内容含模式
$script:handlers["meta-file-contains"] = {
    param($check, $emitIds)
    $p = $check.params
    $file = $csFiles | Where-Object { $_.Name -eq $p.fileName } | Select-Object -First 1
    $text = if ($file) { [System.IO.File]::ReadAllText($file.FullName, [System.Text.Encoding]::UTF8) } else { "" }
    $ok = ($null -ne $file) -and ($text -match $p.pattern)
    return (New-Result $emitIds[0] $ok)
}

# 命名 handler：ModGuid 反向域名式取值（缺失 / 占位符 / 形状三态）
$script:handlers["meta-003-guid"] = {
    param($check, $emitIds)
    $p = $check.params
    $id = $emitIds[0]
    $m = [regex]::Match($meta, $p.pattern)
    if (-not $m.Success) { return (New-Result $id $false ([string]$p.messages.missing)) }
    $value = $m.Groups[1].Value
    if (Test-Placeholder $value) { return (New-Result $id $false "" ([string]$p.messages.placeholder)) }
    $detail = Format-Message $p.messages.detail @{ value = $value }
    return (New-Result $id ($value -match $p.guidPattern) $detail)
}

# 命名 handler：SptVersion tilde 区间——一条检查发射两条结果
#   emitIds[0] = 规则自身（区间声明形状）
#   emitIds[1] = 检查器专用 ID（区间是否覆盖目标版本）
$script:handlers["sptversion-range"] = {
    param($check, $emitIds)
    $p = $check.params
    $rangeId = $emitIds[0]
    $coverId = $emitIds[1]
    $results = @()

    $sv = [regex]::Match($meta, $p.pattern)
    if (-not $sv.Success) {
        $results += New-Result $rangeId $false ([string]$p.messages.missing)
        $results += New-Result $coverId $false ([string]$p.messages.verMissing)
        return $results
    }

    $major = $sv.Groups[1].Value
    $minor = $sv.Groups[2].Value
    $patch = $sv.Groups[3].Value
    $detail = Format-Message $p.messages.detail @{ major = $major; minor = $minor; patch = $patch }
    $results += New-Result $rangeId $true $detail

    $tv = [regex]::Match($TargetSptVersion, $p.targetPattern)
    if (-not $tv.Success) {
        $reason = Format-Message $p.messages.verUnparseable @{ target = $TargetSptVersion }
        $results += New-Result $coverId $false $reason
    } else {
        $covers = ($tv.Groups[1].Value -eq $major) -and
                  ($tv.Groups[2].Value -eq $minor) -and
                  ([int]$tv.Groups[3].Value -ge [int]$patch)
        $coverDetail = Format-Message $p.messages.verDetail @{ target = $TargetSptVersion; major = $major; minor = $minor; patch = $patch }
        $results += New-Result $coverId $covers $coverDetail
    }
    return $results
}

# 命名 handler：csproj <Version> 与 ModMetadata 版本同源（含 paired 单源旁路）
$script:handlers["ver-002-version-single-source"] = {
    param($check, $emitIds)
    $p = $check.params
    $id = $emitIds[0]
    $csprojValue = if ($null -ne $projVer) { [string]$projVer.Value } else { "" }

    $mv = [regex]::Match($meta, $p.metadataVersionPattern)
    if ($mv.Success) {
        $metaValue = $mv.Groups[1].Value
        if ((Test-Placeholder $csprojValue) -or (Test-Placeholder $metaValue)) {
            return (New-Result $id $false "" ([string]$p.messages.placeholder))
        }
        $detail = Format-Message $p.messages.detail @{ csproj = $csprojValue; metadata = $metaValue }
        return (New-Result $id ($csprojValue -eq $metaValue) $detail)
    }
    if ($meta -match $p.singleSourcePattern) {
        return (New-Result $id $true ([string]$p.messages.singleSource))
    }
    return (New-Result $id $false ([string]$p.messages.missing))
}

# 命名 handler：版本三段 semver（缺失 / 占位符 / 形状三态）
$script:handlers["meta-005-version-semver"] = {
    param($check, $emitIds)
    $p = $check.params
    $id = $emitIds[0]
    if ($null -eq $projVer) {
        return (New-Result $id $false ([string]$p.messages.missing))
    }
    if (Test-Placeholder $projVer.Value) {
        $reason = Format-Message $p.messages.placeholder @{ value = $projVer.Value; source = $projVer.Source }
        return (New-Result $id $false "" $reason)
    }
    $detail = Format-Message $p.messages.detail @{ value = $projVer.Value; source = $projVer.Source }
    return (New-Result $id ($projVer.Value -match $p.pattern) $detail)
}

# 命名 handler：客户端插件基类（BepInEx 5 / 6 两态）
$script:handlers["cli-001-plugin-base"] = {
    param($check, $emitIds)
    $p = $check.params
    $id = $emitIds[0]
    if ($src -match $p.primaryPattern) { return (New-Result $id $true ([string]$p.messages.primary)) }
    if ($src -match $p.secondaryPattern) { return (New-Result $id $true ([string]$p.messages.secondary)) }
    return (New-Result $id $false ([string]$p.messages.missing))
}

# 命名 handler：配置类不得标注 [Injectable]
$script:handlers["cfg-004-injectable-config"] = {
    param($check, $emitIds)
    $p = $check.params
    $bad = @()
    foreach ($m in [regex]::Matches($src, $p.pattern)) {
        if ($m.Groups[1].Value -match $p.namePattern) { $bad += $m.Groups[1].Value }
    }
    $detail = if ($bad.Count -gt 0) {
        Format-Message $p.messages.bad @{ list = ($bad -join ",") }
    } else {
        [string]$p.messages.ok
    }
    return (New-Result $emitIds[0] ($bad.Count -eq 0) $detail)
}

# 命名 handler：Harmony 生命周期（未使用 -> N/A PASS；使用 -> 需 patch + 撤销）
# N/A rationale: the rule governs the Harmony create/patch/undo lifecycle.
# A client mod that never instantiates Harmony has no such lifecycle to govern,
# so the rule's precondition is absent and it PASSes. Detection covers the
# common ways Harmony shows up (`new Harmony(`, the fully qualified
# `HarmonyLib.Harmony`, and a `PatchAll(` call) so that a mod that DOES use
# Harmony cannot slip through the N/A branch. With Harmony in use -> require a
# patch call plus a real teardown declaration: `UnpatchSelf` or an
# `override bool Unload(...)` method body (5.0 BasePlugin). Bare `Unload(` calls
# or unrelated `.Dispose()` are NOT accepted: they can be a method invocation or
# an unrelated component and would let the rule pass without any teardown.
$script:handlers["cli-007-harmony-lifecycle"] = {
    param($check, $emitIds)
    $p = $check.params
    $id = $emitIds[0]
    $uses = $false
    foreach ($pattern in @($p.usagePatterns)) { if ($src -match $pattern) { $uses = $true; break } }
    if (-not $uses) { return (New-Result $id $true ([string]$p.messages.na)) }

    $hasPatch = $src -match $p.patchPattern
    $hasUnpatch = $false
    foreach ($pattern in @($p.unpatchPatterns)) { if ($src -match $pattern) { $hasUnpatch = $true; break } }
    $detail = Format-Message $p.messages.detail @{ harmony = $uses; patch = $hasPatch; unpatch = $hasUnpatch }
    return (New-Result $id ($hasPatch -and $hasUnpatch) $detail)
}

# ---- registry 驱动执行（顺序 = 注册表顺序 = 改造前输出顺序） -------------------

foreach ($rule in @($registry.rules)) {
    if ($rule.checkable -ne $true) { continue }
    $check = $rule.check
    if ($check.kind -eq "server" -and -not $isServer) { continue }
    if ($check.kind -eq "client" -and -not $isClient) { continue }

    $handler = $script:handlers[[string]$check.handler]
    if ($null -eq $handler) {
        Write-Error "unknown check handler '$($check.handler)' for rule $($rule.id)"
    }

    $emitIds = @([string]$rule.id)
    if ($null -ne $check.params.emits) { $emitIds = @($check.params.emits) }

    foreach ($result in @(& $handler $check $emitIds)) {
        Resolve-Status $result.Id $result.Ok $result.Detail $result.SkipReason
    }
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

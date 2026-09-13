# Skills invariant: the canonical SPT skill set exists with valid frontmatter,
# and the BGS skills plus the generic devlog/changelog pair are gone.

$ErrorActionPreference = "Stop"

. (Join-Path $PSScriptRoot "_assert.ps1")

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
$skillsRoot = Join-Path $repoRoot "skills"

$requiredSkills = @(
    "using-spt-modding-superpowers",
    "setting-up-spt-modding-environment",
    "maintaining-spt-modding-environment",
    "evaluating-spt-mods",
    "interpreting-spt-mod-instructions",
    "curating-spt-modpack",
    "building-spt-modpack",
    "testing-spt-modpack",
    "diagnosing-spt-problems",
    "spt-conflict-audit",
    "spt-mcp-automation",
    "writing-spt-mod",
    "writing-spt-modpack-devlog",
    "writing-spt-modpack-changelog"
)

function Get-Frontmatter {
    param([string]$Content)

    $match = [regex]::Match($Content, "(?s)\A---\s*\r?\n(.*?)\r?\n---")
    if (-not $match.Success) {
        return $null
    }

    return $match.Groups[1].Value
}

foreach ($skill in $requiredSkills) {
    $skillFile = Join-Path $skillsRoot (Join-Path $skill "SKILL.md")
    if (-not (Test-Path -LiteralPath $skillFile)) {
        Add-BootstrapFailure "missing required SPT skill: skills/$skill/SKILL.md"
        continue
    }

    $content = Get-Content -LiteralPath $skillFile -Raw
    $frontmatter = Get-Frontmatter -Content $content
    if ($null -eq $frontmatter) {
        Add-BootstrapFailure "skills/$skill/SKILL.md has no YAML frontmatter block"
        continue
    }

    $nameMatch = [regex]::Match($frontmatter, "(?m)^name:\s*(.+?)\s*$")
    if (-not $nameMatch.Success) {
        Add-BootstrapFailure "skills/$skill/SKILL.md frontmatter is missing 'name:'"
    } else {
        $declaredName = $nameMatch.Groups[1].Value.Trim().Trim('"', "'")
        if ($declaredName -ne $skill) {
            Add-BootstrapFailure "skills/$skill/SKILL.md frontmatter name '$declaredName' does not match directory name '$skill'"
        }
    }

    $descriptionMatch = [regex]::Match($frontmatter, "(?m)^description:\s*(.+?)\s*$")
    if (-not $descriptionMatch.Success -or [string]::IsNullOrWhiteSpace($descriptionMatch.Groups[1].Value)) {
        Add-BootstrapFailure "skills/$skill/SKILL.md frontmatter is missing a non-empty 'description:'"
    }
}

$absentSkillDirs = @(
    # Generic pair superseded by the SPT-flavored devlog/changelog.
    "writing-modpack-devlog",
    "writing-modpack-changelog",
    # BGS skills whose directory names do not contain "bgs", so the substring
    # scan below cannot catch them.
    "maintaining-modding-environments",
    "interpreting-mod-author-instructions",
    "xedit-automation",
    "xedit-conflict-audit"
)

foreach ($skill in $absentSkillDirs) {
    $skillDir = Join-Path $skillsRoot $skill
    if (Test-Path -LiteralPath $skillDir) {
        Add-BootstrapFailure "skill directory should be removed (BGS or superseded generic): skills/$skill"
    }
}

if (Test-Path -LiteralPath $skillsRoot) {
    $bgsDirs = @(Get-ChildItem -LiteralPath $skillsRoot -Directory | Where-Object { $_.Name -match "(?i)bgs" })
    foreach ($dir in $bgsDirs) {
        Add-BootstrapFailure "BGS skill directory should be removed: skills/$($dir.Name)"
    }
}

Complete-BootstrapCheck -SuccessMessage "SPT skill set present with valid frontmatter; BGS and generic skills gone."

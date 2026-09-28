# Templates invariant: the SPT server-mod and client-mod scaffolds are present
# and complete (manifest, readme, sources, ignore file).

$ErrorActionPreference = "Stop"

. (Join-Path $PSScriptRoot "_assert.ps1")

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path

$templateSets = [ordered]@{
    "templates/server-mod" = @(
        "README.md",
        "ServerModTemplate.csproj",
        "src/ModEntry.cs",
        "src/ModMetadata.cs",
        "src/Services/ExampleService.cs",
        ".gitignore"
    )
    "templates/client-mod" = @(
        "README.md",
        "ClientModTemplate.csproj",
        "src/Plugin.cs",
        "src/Configuration.cs",
        "src/Patches/ExamplePatch.cs",
        ".gitignore"
    )
    "templates/spt5-client-mod" = @(
        "README.md",
        "ClientModTemplate.csproj",
        "src/Plugin.cs",
        "src/Configuration.cs",
        "src/Patches/ExamplePatch.cs",
        ".gitignore"
    )
    "templates/spt5-server-mod" = @(
        "README.md",
        "ServerModTemplate.csproj",
        "src/ModEntry.cs",
        "src/ModMetadata.cs",
        "src/Services/ExampleService.cs",
        "src/Routers/ExampleRouter.cs",
        ".gitignore"
    )
    "templates/spt5-paired-mod" = @(
        "README.md",
        "SPT5PairedModTemplate.sln",
        "Directory.Build.props",
        "Client/Client.csproj",
        "Server/Server.csproj",
        "Shared/Shared.csproj",
        "scripts/pack.ps1",
        ".gitignore"
    )
}

foreach ($template in $templateSets.GetEnumerator()) {
    $templateRoot = Join-Path $repoRoot $template.Key

    if (-not (Test-Path -LiteralPath $templateRoot)) {
        Add-BootstrapFailure "missing template directory: $($template.Key)"
        continue
    }

    $files = @(Get-ChildItem -LiteralPath $templateRoot -Recurse -File -Force)
    if ($files.Count -eq 0) {
        Add-BootstrapFailure "template directory is empty: $($template.Key)"
    }

    foreach ($relative in $template.Value) {
        Assert-PathExists -Path (Join-Path $templateRoot $relative) -Label "$($template.Key)/$relative"
    }

    $manifests = @($files | Where-Object { $_.Extension -eq ".csproj" })
    if ($manifests.Count -eq 0) {
        Add-BootstrapFailure "template has no project manifest (.csproj): $($template.Key)"
    }

    $sources = @($files | Where-Object { $_.Extension -eq ".cs" })
    if ($sources.Count -eq 0) {
        Add-BootstrapFailure "template has no C# scaffold sources (.cs): $($template.Key)"
    }
}

Complete-BootstrapCheck -SuccessMessage "SPT server and client templates carry the expected scaffold."

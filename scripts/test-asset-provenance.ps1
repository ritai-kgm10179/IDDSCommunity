#Requires -Version 7.4

[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$manifestPath = Join-Path $repositoryRoot 'assets\asset-provenance.json'
$imageExtensions = @('.bmp', '.gif', '.ico', '.jpeg', '.jpg', '.png', '.svg')
$legacyNames = '(?i)(setup_banner|realvista|filemakerpro|sharepoint)'

if (-not (Test-Path -LiteralPath $manifestPath -PathType Leaf)) {
    throw 'Asset provenance manifest is missing.'
}

$manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
if ($manifest.schemaVersion -ne 1) {
    throw "Unsupported asset provenance schema version: $($manifest.schemaVersion)"
}

$trackedFiles = @(& git -C $repositoryRoot ls-files)
if ($LASTEXITCODE -ne 0) {
    throw 'Unable to enumerate tracked repository files.'
}
$trackedAssets = @($trackedFiles | Where-Object { [System.IO.Path]::GetExtension($_).ToLowerInvariant() -in $imageExtensions } | Sort-Object)
$manifestAssets = @($manifest.assets | Sort-Object path)
if ($trackedAssets.Count -ne $manifestAssets.Count) {
    throw "Asset manifest contains $($manifestAssets.Count) entries, but Git tracks $($trackedAssets.Count) image assets."
}

for ($index = 0; $index -lt $trackedAssets.Count; $index++) {
    $relativePath = $trackedAssets[$index]
    $entry = $manifestAssets[$index]
    if ($relativePath -ne $entry.path) {
        throw "Asset manifest mismatch: expected '$relativePath', found '$($entry.path)'."
    }
    if ($relativePath -match $legacyNames) {
        throw "Prohibited inherited asset name remains tracked: $relativePath"
    }
    $fullPath = Join-Path $repositoryRoot ($relativePath.Replace('/', [System.IO.Path]::DirectorySeparatorChar))
    $actualHash = [Convert]::ToHexString([System.Security.Cryptography.SHA256]::HashData([System.IO.File]::ReadAllBytes($fullPath))).ToLowerInvariant()
    if ($actualHash -ne $entry.sha256) {
        throw "Asset hash mismatch: $relativePath"
    }
    if ($entry.origin -eq 'project-generated' -and $entry.source -ne 'tools/Generate-OriginalAssets.ps1') {
        throw "Generated asset has an invalid source declaration: $relativePath"
    }
    if ($entry.origin -notin @('project-generated', 'ai-assisted-project-branding')) {
        throw "Asset has an unsupported provenance classification: $relativePath"
    }
}

Write-Host "Verified provenance and SHA-256 hashes for $($trackedAssets.Count) tracked image assets."

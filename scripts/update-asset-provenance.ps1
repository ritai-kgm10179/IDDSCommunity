#Requires -Version 7.4

[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$manifestPath = Join-Path $repositoryRoot 'assets\asset-provenance.json'
$imageExtensions = @('.bmp', '.gif', '.ico', '.jpeg', '.jpg', '.png', '.svg')

$trackedFiles = @(& git -C $repositoryRoot ls-files)
if ($LASTEXITCODE -ne 0) {
    throw 'Unable to enumerate tracked repository files.'
}

$assets = foreach ($relativePath in ($trackedFiles | Sort-Object)) {
    $extension = [System.IO.Path]::GetExtension($relativePath).ToLowerInvariant()
    if ($extension -notin $imageExtensions) {
        continue
    }

    $fullPath = Join-Path $repositoryRoot ($relativePath.Replace('/', [System.IO.Path]::DirectorySeparatorChar))
    $hash = [Convert]::ToHexString([System.Security.Cryptography.SHA256]::HashData([System.IO.File]::ReadAllBytes($fullPath))).ToLowerInvariant()
    $isBrandingIcon = $relativePath -eq 'assets/branding/idds-community.ico'
    [ordered]@{
        path = $relativePath
        sha256 = $hash
        origin = if ($isBrandingIcon) { 'ai-assisted-project-branding' } else { 'project-generated' }
        source = if ($isBrandingIcon) { 'IDDS Community original branding work' } else { 'tools/Generate-OriginalAssets.ps1' }
    }
}

$manifest = [ordered]@{
    schemaVersion = 1
    statement = 'Tracked product image assets are original IDDS Community or AI-assisted project resources and are not inherited artwork.'
    assets = @($assets)
}

$json = $manifest | ConvertTo-Json -Depth 5
[System.IO.File]::WriteAllText($manifestPath, $json + "`r`n", [System.Text.UTF8Encoding]::new($false))
Write-Host "Updated asset provenance for $($manifest.assets.Count) tracked images."

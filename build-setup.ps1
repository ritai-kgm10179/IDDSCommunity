[CmdletBinding()]
param(
    [ValidateSet('win-x64', 'win-arm64')]
    [string] $RuntimeIdentifier = 'win-x64',
    [string] $Configuration = 'Release',
    [ValidatePattern('^\d+\.\d+\.\d+$')]
    [string] $Version = '3.0.0',
    [switch] $Offline
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName 'System.IO.Compression.FileSystem'
$repositoryRoot = $PSScriptRoot
$packageRoot = Join-Path $repositoryRoot "artifacts\setup\idds-community-$Version-$RuntimeIdentifier"
$payloadRoot = Join-Path $repositoryRoot 'artifacts\setup\temp_payload'
$pluginRoot = Join-Path $payloadRoot 'Plugins'
$diagnosticsRoot = Join-Path $payloadRoot 'Tools\DatabaseDiagnostics'

if (Test-Path -LiteralPath $packageRoot) {
    try {
        Remove-Item -LiteralPath $packageRoot -Recurse -Force -ErrorAction Stop
    }
    catch {
        Start-Sleep -Milliseconds 1000
        Remove-Item -LiteralPath $packageRoot -Recurse -Force -ErrorAction SilentlyContinue
    }
}
if (Test-Path -LiteralPath $payloadRoot) {
    Remove-Item -LiteralPath $payloadRoot -Recurse -Force -ErrorAction SilentlyContinue
}
New-Item -ItemType Directory -Path $packageRoot, $payloadRoot, $pluginRoot, $diagnosticsRoot -Force | Out-Null

# 一般方案還原不會建立執行階段專屬資產；以單一節點鎖定還原並停用節點重用，
# 避免 Windows SDK 在大量專案還原時留下不必要的 MSBuild 子程序。
$env:MSBUILDDISABLENODEREUSE = '1'
$restoreArguments = @('--locked-mode', '--disable-parallel', '-m:1', '--nologo')
if ($Offline) {
    $offlinePackages = if ([string]::IsNullOrWhiteSpace($env:NUGET_PACKAGES)) {
        Join-Path ([Environment]::GetFolderPath([Environment+SpecialFolder]::UserProfile)) '.nuget\packages'
    } else {
        $env:NUGET_PACKAGES
    }
    $restoreArguments += @('-p:NuGetAudit=false', '--source', $offlinePackages, '--source', (Join-Path $env:ProgramFiles 'dotnet\library-packs'))
}
dotnet restore (Join-Path $repositoryRoot 'IDDSCommunity.slnx') @restoreArguments
if ($LASTEXITCODE -ne 0) { throw '執行階段相依套件還原失敗。' }

$commonArguments = @('--configuration', $Configuration, '--runtime', $RuntimeIdentifier, '--self-contained', 'true', '-p:PublishSingleFile=true', '-p:IncludeNativeLibrariesForSelfExtract=true', '-p:EnableCompressionInSingleFile=true', '--no-restore', '--nologo', '--disable-build-servers', '-m:1')
$pluginArguments = @('--configuration', $Configuration, '--self-contained', 'false', '--no-restore', '--nologo', '--disable-build-servers', '-m:1')

dotnet publish (Join-Path $repositoryRoot 'src\IDDSCommunity.IntrusionDetection.Service\IDDSCommunity.IntrusionDetection.Service.csproj') @commonArguments --output $payloadRoot
if ($LASTEXITCODE -ne 0) { throw '服務發佈失敗。' }
dotnet publish (Join-Path $repositoryRoot 'src\IDDSCommunity.IntrusionDetection.Admin\IDDSCommunity.IntrusionDetection.Admin.csproj') @commonArguments --output $payloadRoot
if ($LASTEXITCODE -ne 0) { throw '管理介面發佈失敗。' }
dotnet publish (Join-Path $repositoryRoot 'tools\IDDSCommunity.DatabaseDiagnostics\IDDSCommunity.DatabaseDiagnostics.csproj') @commonArguments --output $diagnosticsRoot
if ($LASTEXITCODE -ne 0) { throw '資料庫診斷工具發佈失敗。' }
$psModuleRoot = Join-Path $payloadRoot 'PowerShell\Modules\IDDSCommunity'
New-Item -ItemType Directory -Path $psModuleRoot -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $repositoryRoot 'tools\IDDSCommunity.PowerShell\*') -Destination $psModuleRoot -Recurse -Force
Copy-Item -LiteralPath (Join-Path $repositoryRoot 'README.md') -Destination $payloadRoot -Force
Copy-Item -LiteralPath (Join-Path $repositoryRoot 'LICENSE') -Destination $payloadRoot -Force
Copy-Item -LiteralPath (Join-Path $repositoryRoot 'FORK-NOTICE.md') -Destination $payloadRoot -Force
Copy-Item -LiteralPath (Join-Path $repositoryRoot 'LICENSE-PROVENANCE.md') -Destination $payloadRoot -Force
Copy-Item -LiteralPath (Join-Path $repositoryRoot 'ASSET-PROVENANCE.md') -Destination $payloadRoot -Force
Copy-Item -LiteralPath (Join-Path $repositoryRoot 'assets\asset-provenance.json') -Destination $payloadRoot -Force
Copy-Item -LiteralPath (Join-Path $repositoryRoot 'THIRD-PARTY-NOTICES.md') -Destination $payloadRoot -Force
$userGuideSource = Join-Path $repositoryRoot 'docs\USER-GUIDE.zh-TW.md'
$userGuideEnSource = Join-Path $repositoryRoot 'docs\USER-GUIDE.en-US.md'
if (Test-Path -LiteralPath $userGuideSource) {
    Copy-Item -LiteralPath $userGuideSource -Destination (Join-Path $payloadRoot 'USER-GUIDE.md') -Force
    Copy-Item -LiteralPath $userGuideSource -Destination (Join-Path $payloadRoot 'USER-GUIDE.zh-TW.md') -Force
}
if (Test-Path -LiteralPath $userGuideEnSource) {
    Copy-Item -LiteralPath $userGuideEnSource -Destination (Join-Path $payloadRoot 'USER-GUIDE.en-US.md') -Force
}

$pluginProjects = @(
    'IDDSCommunity.Agents.ActiveDirectory',
    'IDDSCommunity.Agents.Authentication.Common',
    'IDDSCommunity.Agents.FileMaker', 'IDDSCommunity.Agents.FileZilla', 'IDDSCommunity.Agents.FtpServer', 'IDDSCommunity.Agents.Honeypot', 'IDDSCommunity.Agents.IisAuthentication',
    'IDDSCommunity.Agents.MailServer', 'IDDSCommunity.Agents.MySql', 'IDDSCommunity.Agents.OpenSsh',
    'IDDSCommunity.Agents.PostgreSql', 'IDDSCommunity.Agents.Radius', 'IDDSCommunity.Agents.RemoteDesktopGateway', 'IDDSCommunity.Agents.SqlServer',
    'IDDSCommunity.Agents.TechnitiumDns', 'IDDSCommunity.Agents.TerminalServer', 'IDDSCommunity.Agents.WebSecurity', 'IDDSCommunity.Agents.WindowsDns',
    'IDDSCommunity.Agents.WindowsNetworkLogon', 'IDDSCommunity.Agents.WinRm',
    'IDDSCommunity.IntrusionDetection.Base.Plugins'
)
foreach ($projectName in $pluginProjects) {
    $project = Get-ChildItem -Path $repositoryRoot -Recurse -Filter "$projectName.csproj" | Select-Object -First 1
    if (-not $project) { throw "找不到專案：$projectName.csproj" }
    dotnet publish $project.FullName @pluginArguments --output $pluginRoot
    if ($LASTEXITCODE -ne 0) { throw "代理程式發佈失敗：$projectName" }
}

$disallowedWinDivertFiles = @(Get-ChildItem -LiteralPath $payloadRoot -Recurse -File | Where-Object { $_.Name -like 'WinDivert*' })
if ($disallowedWinDivertFiles.Count -ne 0) {
    throw "發行內容不得包含 WinDivert：$($disallowedWinDivertFiles.FullName -join ', ')"
}

# Compress payload directory into a zip archive and embed it into the Setup project for a 100% self-contained Single EXE
$setupProjectDir = Join-Path $repositoryRoot 'src\IDDSCommunity.IntrusionDetection.Setup'
$setupZipPath = Join-Path $setupProjectDir 'payload.zip'
$setupTempOut = Join-Path $repositoryRoot 'artifacts\setup\temp_setup_out'
if (Test-Path -LiteralPath $setupZipPath) { Remove-Item -LiteralPath $setupZipPath -Force }
if (Test-Path -LiteralPath $setupTempOut) { Remove-Item -LiteralPath $setupTempOut -Recurse -Force -ErrorAction SilentlyContinue }

for ($attempt = 1; $attempt -le 5; $attempt++) {
    try {
        [System.IO.Compression.ZipFile]::CreateFromDirectory($payloadRoot, $setupZipPath, [System.IO.Compression.CompressionLevel]::Optimal, $false)
        break
    }
    catch {
        if ($attempt -eq 5) { throw }
        Start-Sleep -Milliseconds 1000
    }
}

$setupArguments = @('--configuration', $Configuration, '--runtime', $RuntimeIdentifier, '--self-contained', 'true', '-p:PublishSingleFile=true', '-p:IncludeNativeLibrariesForSelfExtract=true', '-p:EnableCompressionInSingleFile=true', '--no-restore', '--nologo', '--disable-build-servers', '-m:1')

try {
    dotnet publish (Join-Path $setupProjectDir 'IDDSCommunity.IntrusionDetection.Setup.csproj') @setupArguments --output $setupTempOut
    if ($LASTEXITCODE -ne 0) { throw '安裝程式發佈失敗。' }

    if (-not (Test-Path -LiteralPath $packageRoot)) {
        New-Item -ItemType Directory -Path $packageRoot -Force | Out-Null
    }
    Copy-Item -LiteralPath (Join-Path $setupTempOut 'Setup.exe') -Destination $packageRoot -Force
}
finally {
    if (Test-Path -LiteralPath $setupZipPath) { Remove-Item -LiteralPath $setupZipPath -Force }
    if (Test-Path -LiteralPath $setupTempOut) { Remove-Item -LiteralPath $setupTempOut -Recurse -Force -ErrorAction SilentlyContinue }
    if (Test-Path -LiteralPath $payloadRoot) { Remove-Item -LiteralPath $payloadRoot -Recurse -Force -ErrorAction SilentlyContinue }
}

$userGuideSource = Join-Path $repositoryRoot 'USER-GUIDE.md'
$userGuideEnSource = Join-Path $repositoryRoot 'USER-GUIDE.en-US.md'

Copy-Item -LiteralPath (Join-Path $repositoryRoot 'LICENSE') -Destination $packageRoot -Force
if (Test-Path -LiteralPath $userGuideSource) {
    Copy-Item -LiteralPath $userGuideSource -Destination (Join-Path $packageRoot 'USER-GUIDE.md') -Force
}
if (Test-Path -LiteralPath $userGuideEnSource) {
    Copy-Item -LiteralPath $userGuideEnSource -Destination (Join-Path $packageRoot 'USER-GUIDE.en-US.md') -Force
}

# --- SBOM 生成 (SPDX 3.0 與相容性 SPDX 2.2) ---
$sbomToolDir = Join-Path $repositoryRoot 'artifacts\tools\sbom-tool'
if (-not (Test-Path (Join-Path $sbomToolDir 'sbom-tool.exe'))) {
    if ($Offline) { throw '離線封裝需要預先安裝 Microsoft.Sbom.DotNetTool。' }
    dotnet tool install --tool-path $sbomToolDir Microsoft.Sbom.DotNetTool --version 4.1.5 | Out-Null
}
$sbomTool = Join-Path $sbomToolDir 'sbom-tool.exe'
$namespace = "https://github.com/ritai-kgm10179/IDDSCommunity/releases/tag/v$Version/$RuntimeIdentifier"
$sbomStaging = Join-Path $repositoryRoot "artifacts\setup\temp_sbom_$RuntimeIdentifier"
if (Test-Path -LiteralPath $sbomStaging) { Remove-Item -LiteralPath $sbomStaging -Recurse -Force -ErrorAction SilentlyContinue }

$sboms = @(
    @{ Info = 'SPDX:3.0'; Directory = 'spdx_3.0'; File = (Join-Path $repositoryRoot "artifacts\setup\idds-community-$Version-$RuntimeIdentifier.spdx-3.0.json") },
    @{ Info = 'SPDX:2.2'; Directory = 'spdx_2.2'; File = (Join-Path $repositoryRoot "artifacts\setup\idds-community-$Version-$RuntimeIdentifier.spdx-2.2.json") }
)

foreach ($definition in $sboms) {
    $staging = Join-Path $sbomStaging $definition.Directory
    New-Item -ItemType Directory -Path $staging -Force | Out-Null
    & $sbomTool generate -b $packageRoot -bc $repositoryRoot -cd '--DirectoryExclusionList **/artifacts/**' -m $staging -pn "IDDS Community $RuntimeIdentifier" -pv $Version -ps 'Organization: IDDS Community' -nsb "$namespace/$($definition.Directory)" -mi $definition.Info
    if ($LASTEXITCODE -ne 0) { throw "$($definition.Info) SBOM 產生失敗。" }

    $generatedSbom = Join-Path $staging "_manifest\$($definition.Directory)\manifest.spdx.json"
    if (-not (Test-Path -LiteralPath $generatedSbom)) { throw "$($definition.Info) SBOM 檔案未生成。" }

}

foreach ($definition in $sboms) {
    $generatedSbom = Join-Path (Join-Path $sbomStaging $definition.Directory) "_manifest\$($definition.Directory)\manifest.spdx.json"
    # 內嵌至安裝套件目錄
    $packageSbomDirectory = Join-Path $packageRoot "_manifest\$($definition.Directory)"
    New-Item -ItemType Directory -Path $packageSbomDirectory -Force | Out-Null
    Copy-Item -LiteralPath $generatedSbom -Destination (Join-Path $packageSbomDirectory 'manifest.spdx.json') -Force
    # 複製至外部 Release 附件目錄
    Copy-Item -LiteralPath $generatedSbom -Destination $definition.File -Force
}

if (Test-Path -LiteralPath $sbomStaging) { Remove-Item -LiteralPath $sbomStaging -Recurse -Force -ErrorAction SilentlyContinue }

# --- Zip 封裝與 SHA-256 雜湊生成 ---
$archive = Join-Path $repositoryRoot "artifacts\setup\idds-community-$Version-$RuntimeIdentifier.zip"
if (Test-Path -LiteralPath $archive) { Remove-Item -LiteralPath $archive -Force }

for ($attempt = 1; $attempt -le 5; $attempt++) {
    try {
        [System.IO.Compression.ZipFile]::CreateFromDirectory($packageRoot, $archive, [System.IO.Compression.CompressionLevel]::Optimal, $false)
        break
    }
    catch {
        if ($attempt -eq 5) { throw }
        Start-Sleep -Milliseconds 1000
    }
}

$utf8NoBom = New-Object System.Text.UTF8Encoding($false)
foreach ($subject in @($archive, $sboms[0].File, $sboms[1].File)) {
    $hash = (Get-FileHash -LiteralPath $subject -Algorithm SHA256).Hash.ToLowerInvariant()
    $hashContent = "$hash *$(Split-Path -Leaf $subject)`r`n"
    [System.IO.File]::WriteAllText("$subject.sha256", $hashContent, $utf8NoBom)
}

Write-Host "安裝套件、雙版本 SBOM 與 SHA-256 已建立：$packageRoot"

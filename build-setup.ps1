[CmdletBinding()]
param(
    [ValidateSet('win-x64', 'win-arm64')]
    [string] $RuntimeIdentifier = 'win-x64',
    [string] $Configuration = 'Release',
    [ValidatePattern('^\d+\.\d+\.\d+$')]
    [string] $Version = '3.0.0'
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = $PSScriptRoot
$packageRoot = Join-Path $repositoryRoot "artifacts\setup\idds-community-$Version-$RuntimeIdentifier"
$payloadRoot = Join-Path $repositoryRoot 'artifacts\setup\temp_payload'
$pluginRoot = Join-Path $payloadRoot 'Plugins'

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
New-Item -ItemType Directory -Path $packageRoot, $payloadRoot, $pluginRoot -Force | Out-Null

# 一般方案還原不會建立執行階段專屬資產；以單一節點還原並停用節點重用，
# 避免 Windows SDK 在大量專案還原時留下不必要的 MSBuild 子程序。
$env:MSBUILDDISABLENODEREUSE = '1'
dotnet restore (Join-Path $repositoryRoot 'IDDSCommunity.slnx') --runtime $RuntimeIdentifier --disable-parallel -m:1 -p:NuGetAudit=false --nologo
if ($LASTEXITCODE -ne 0) { throw '執行階段相依套件還原失敗。' }

# Vulnerability auditing belongs to the explicit restore/CI step. Publishing uses the already-audited lock state.
$commonArguments = @('--configuration', $Configuration, '--runtime', $RuntimeIdentifier, '--self-contained', 'true', '-p:PublishSingleFile=true', '-p:IncludeNativeLibrariesForSelfExtract=true', '-p:EnableCompressionInSingleFile=true', '--no-restore', '-p:NuGetAudit=false', '--nologo', '--disable-build-servers', '-m:1')
$pluginArguments = @('--configuration', $Configuration, '--self-contained', 'false', '--no-restore', '-p:NuGetAudit=false', '--nologo', '--disable-build-servers', '-m:1')

dotnet publish (Join-Path $repositoryRoot 'src\IDDSCommunity.IntrusionDetection.Service\IDDSCommunity.IntrusionDetection.Service.csproj') @commonArguments --output $payloadRoot
if ($LASTEXITCODE -ne 0) { throw '服務發佈失敗。' }
dotnet publish (Join-Path $repositoryRoot 'src\IDDSCommunity.IntrusionDetection.Admin\IDDSCommunity.IntrusionDetection.Admin.csproj') @commonArguments --output $payloadRoot
if ($LASTEXITCODE -ne 0) { throw '管理介面發佈失敗。' }
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
    'IDDSCommunity.Agents.Authentication.Common',
    'IDDSCommunity.Agents.FileMaker', 'IDDSCommunity.Agents.FileZilla', 'IDDSCommunity.Agents.FtpServer', 'IDDSCommunity.Agents.IisAuthentication',
    'IDDSCommunity.Agents.MailServer', 'IDDSCommunity.Agents.MySql', 'IDDSCommunity.Agents.OpenSsh',
    'IDDSCommunity.Agents.PostgreSql', 'IDDSCommunity.Agents.Radius', 'IDDSCommunity.Agents.SqlServer',
    'IDDSCommunity.Agents.TechnitiumDns', 'IDDSCommunity.Agents.TerminalServer', 'IDDSCommunity.Agents.WebSecurity', 'IDDSCommunity.Agents.WindowsDns',
    'IDDSCommunity.Agents.WindowsNetworkLogon',
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

Compress-Archive -Path "$payloadRoot\*" -DestinationPath $setupZipPath -Force

$setupArguments = @('--configuration', $Configuration, '--runtime', $RuntimeIdentifier, '--self-contained', 'true', '-p:PublishSingleFile=true', '-p:IncludeNativeLibrariesForSelfExtract=true', '-p:EnableCompressionInSingleFile=true', '-p:NuGetAudit=false', '--nologo', '--disable-build-servers', '-m:1')

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

Copy-Item -LiteralPath (Join-Path $repositoryRoot 'LICENSE') -Destination $packageRoot -Force
if (Test-Path -LiteralPath $userGuideSource) {
    Copy-Item -LiteralPath $userGuideSource -Destination (Join-Path $packageRoot 'USER-GUIDE.md') -Force
}
if (Test-Path -LiteralPath $userGuideEnSource) {
    Copy-Item -LiteralPath $userGuideEnSource -Destination (Join-Path $packageRoot 'USER-GUIDE.en-US.md') -Force
}
Write-Host "安裝套件已建立：$packageRoot"

[CmdletBinding()]
param(
    [ValidateSet('win-x64', 'win-arm64')]
    [string] $RuntimeIdentifier = 'win-x64',
    [string] $Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = $PSScriptRoot
$packageRoot = Join-Path $repositoryRoot "artifacts\setup\idds-community-3.0.0-$RuntimeIdentifier"
$payloadRoot = Join-Path $packageRoot 'payload'
$pluginRoot = Join-Path $payloadRoot 'Plugins'

if (Test-Path -LiteralPath $packageRoot) {
    Remove-Item -LiteralPath $packageRoot -Recurse -Force
}
New-Item -ItemType Directory -Path $payloadRoot, $pluginRoot -Force | Out-Null

# Vulnerability auditing belongs to the explicit restore/CI step. Publishing uses the already-audited lock state.
$commonArguments = @('--configuration', $Configuration, '--runtime', $RuntimeIdentifier, '--self-contained', 'true', '--no-restore', '-p:NuGetAudit=false', '--nologo', '--disable-build-servers', '-m:1')
dotnet publish (Join-Path $repositoryRoot 'IDDSCommunity.IntrusionDetection.Service\IDDSCommunity.IntrusionDetection.Service.csproj') @commonArguments --output $payloadRoot
if ($LASTEXITCODE -ne 0) { throw '服務發佈失敗。' }
dotnet publish (Join-Path $repositoryRoot 'IDDSCommunity.IntrusionDetection.Admin\IDDSCommunity.IntrusionDetection.Admin.csproj') @commonArguments --output $payloadRoot
if ($LASTEXITCODE -ne 0) { throw '管理介面發佈失敗。' }
Copy-Item -LiteralPath (Join-Path $repositoryRoot 'README.md') -Destination $payloadRoot

$pluginProjects = @(
    'IDDSCommunity.Agents.FileMaker', 'IDDSCommunity.Agents.FtpServer', 'IDDSCommunity.Agents.MailServer',
    'IDDSCommunity.Agents.MySql', 'IDDSCommunity.Agents.SqlServer',
    'IDDSCommunity.Agents.TerminalServer', 'IDDSCommunity.Agents.WebSecurity', 'IDDSCommunity.Agents.WindowsDns',
    'IDDSCommunity.IntrusionDetection.Base'
)
foreach ($projectName in $pluginProjects) {
    $project = Get-ChildItem -LiteralPath (Join-Path $repositoryRoot $projectName) -Filter '*.csproj' | Select-Object -First 1
    dotnet publish $project.FullName @commonArguments --output $pluginRoot
    if ($LASTEXITCODE -ne 0) { throw "代理程式發佈失敗：$projectName" }
}

dotnet publish (Join-Path $repositoryRoot 'IDDSCommunity.IntrusionDetection.Setup\IDDSCommunity.IntrusionDetection.Setup.csproj') @commonArguments --output $packageRoot
if ($LASTEXITCODE -ne 0) { throw '安裝程式發佈失敗。' }
Copy-Item -LiteralPath (Join-Path $repositoryRoot 'LICENSE') -Destination $packageRoot
Copy-Item -LiteralPath (Join-Path $repositoryRoot 'FORK-NOTICE.md') -Destination $packageRoot
Copy-Item -LiteralPath (Join-Path $repositoryRoot 'LICENSE-PROVENANCE.md') -Destination $packageRoot
Copy-Item -LiteralPath (Join-Path $repositoryRoot 'THIRD-PARTY-NOTICES.md') -Destination $packageRoot
Write-Host "安裝套件已建立：$packageRoot"

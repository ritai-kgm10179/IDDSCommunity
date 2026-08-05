#Requires -Version 7.4
#Requires -RunAsAdministrator

[CmdletBinding()]
param(
    [Parameter()]
    [string]$ServiceName
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$previousOptIn = $env:IDDSCOMMUNITY_RUN_PRIVILEGED_TESTS
$previousServiceName = $env:IDDSCOMMUNITY_TEST_SERVICE_NAME

try {
    $env:IDDSCOMMUNITY_RUN_PRIVILEGED_TESTS = '1'
    if ([string]::IsNullOrWhiteSpace($ServiceName)) {
        Remove-Item Env:IDDSCOMMUNITY_TEST_SERVICE_NAME -ErrorAction SilentlyContinue
    }
    else {
        if (-not $ServiceName.StartsWith('IDDSCommunity Integration Test', [StringComparison]::Ordinal)) {
            throw "ServiceName must start with 'IDDSCommunity Integration Test'."
        }
        $env:IDDSCOMMUNITY_TEST_SERVICE_NAME = $ServiceName
    }

    dotnet test "$repositoryRoot\IDDSCommunity.IntrusionDetection.Service.Test\IDDSCommunity.IntrusionDetection.Service.Test.csproj" `
        --filter 'TestCategory=PrivilegedWindows' `
        --disable-build-servers `
        -m:1 `
        -p:UseSharedCompilation=false `
        --verbosity minimal
    if ($LASTEXITCODE -ne 0) {
        throw "Privileged Windows integration tests failed with exit code $LASTEXITCODE."
    }
}
finally {
    $env:IDDSCOMMUNITY_RUN_PRIVILEGED_TESTS = $previousOptIn
    $env:IDDSCOMMUNITY_TEST_SERVICE_NAME = $previousServiceName
}

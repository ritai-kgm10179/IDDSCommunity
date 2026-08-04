#Requires -Version 7.4
#Requires -RunAsAdministrator

[CmdletBinding()]
param(
    [Parameter()]
    [string]$ServiceName
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$previousOptIn = $env:CYBERARMS_RUN_PRIVILEGED_TESTS
$previousServiceName = $env:CYBERARMS_TEST_SERVICE_NAME

try {
    $env:CYBERARMS_RUN_PRIVILEGED_TESTS = '1'
    if ([string]::IsNullOrWhiteSpace($ServiceName)) {
        Remove-Item Env:CYBERARMS_TEST_SERVICE_NAME -ErrorAction SilentlyContinue
    }
    else {
        if (-not $ServiceName.StartsWith('Cyberarms Integration Test', [StringComparison]::Ordinal)) {
            throw "ServiceName must start with 'Cyberarms Integration Test'."
        }
        $env:CYBERARMS_TEST_SERVICE_NAME = $ServiceName
    }

    dotnet test "$repositoryRoot\Cyberarms.IntrusionDetection.Service.Test\Cyberarms.IntrusionDetection.Service.Test.csproj" `
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
    $env:CYBERARMS_RUN_PRIVILEGED_TESTS = $previousOptIn
    $env:CYBERARMS_TEST_SERVICE_NAME = $previousServiceName
}

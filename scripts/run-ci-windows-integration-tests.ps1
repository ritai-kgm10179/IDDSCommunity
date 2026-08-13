#Requires -Version 7.4
#Requires -RunAsAdministrator

[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$serviceName = 'IDDSCommunity Integration Test Runtime'
$serviceExecutable = Join-Path $repositoryRoot 'src\IDDSCommunity.IntrusionDetection.Service\bin\Release\net10.0-windows\IDDSCommunity.IntrusionDetection.Service.exe'
$eventSource = 'IDDS Community'
$eventLogName = 'Application'
$createdEventSource = $false

if (-not (Test-Path -LiteralPath $serviceExecutable -PathType Leaf)) {
    throw "Integration-test service executable was not found: $serviceExecutable"
}

function Invoke-ServiceControl {
    param([Parameter(Mandatory)][string[]] $Arguments, [switch] $AcceptFailure)
    & "$env:SystemRoot\System32\sc.exe" @Arguments
    if (-not $AcceptFailure -and $LASTEXITCODE -ne 0) {
        throw "sc.exe failed with exit code ${LASTEXITCODE}: $($Arguments -join ' ')"
    }
}

try {
    Invoke-ServiceControl -Arguments @('delete', $serviceName) -AcceptFailure
    if (-not [System.Diagnostics.EventLog]::SourceExists($eventSource)) {
        [System.Diagnostics.EventLog]::CreateEventSource($eventSource, $eventLogName)
        $createdEventSource = $true
    }
    Invoke-ServiceControl -Arguments @('create', $serviceName, 'binPath=', "`"$serviceExecutable`"", 'start=', 'demand', 'DisplayName=', $serviceName)
    & (Join-Path $PSScriptRoot 'run-privileged-windows-tests.ps1') -ServiceName $serviceName
    if ($LASTEXITCODE -ne 0) { throw "Privileged integration tests failed with exit code $LASTEXITCODE." }
}
finally {
    Invoke-ServiceControl -Arguments @('stop', $serviceName) -AcceptFailure
    Invoke-ServiceControl -Arguments @('delete', $serviceName) -AcceptFailure
    if ($createdEventSource -and [System.Diagnostics.EventLog]::SourceExists($eventSource)) {
        [System.Diagnostics.EventLog]::DeleteEventSource($eventSource)
    }
}

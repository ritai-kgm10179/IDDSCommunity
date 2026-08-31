@{
    RootModule = 'IDDSCommunity.psm1'
    ModuleVersion = '1.0.0'
    GUID = 'a8f1234b-8422-4917-a63e-63f5383f982a'
    Author = 'IDDS Community Team'
    CompanyName = 'IDDS Community'
    Copyright = '(c) 2026 IDDS Community. All rights reserved.'
    Description = 'Official PowerShell management and automation module for IDDS Community Intrusion Detection System.'
    PowerShellVersion = '7.0'
    FunctionsToExport = @(
        'Get-IddsStatus',
        'Get-IddsBlockedIp',
        'Block-IddsIp',
        'Unblock-IddsIp',
        'Add-IddsSafeNetwork',
        'Get-IddsSafeNetwork',
        'Remove-IddsSafeNetwork',
        'Get-IddsCloudPerimeter',
        'Test-IddsHoneyAccount',
        'Invoke-IddsCisScan',
        'Export-IddsStixBundle',
        'Export-IddsIso27001Report',
        'Test-IddsNotification'
    )
    CmdletsToExport = @()
    VariablesToExport = '*'
    AliasesToExport = @()
    PrivateData = @{
        PSData = @{
            Tags = @('Security', 'Firewall', 'IntrusionDetection', 'HIDS', 'STIX', 'ISO27001')
            LicenseUri = 'https://github.com/ritai-kgm10179/IDDSCommunity/blob/main/LICENSE'
            ProjectUri = 'https://github.com/ritai-kgm10179/IDDSCommunity'
        }
    }
}

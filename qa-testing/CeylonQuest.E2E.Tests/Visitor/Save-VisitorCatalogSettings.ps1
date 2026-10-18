[CmdletBinding()]
param(
    [string]$ConfigPath = (Join-Path $PSScriptRoot 'TestResults\visitor-settings.local.json'),
    [switch]$ResetPassword
)

$ErrorActionPreference = 'Stop'
if ($env:OS -ne 'Windows_NT') { throw 'Password encryption requires Windows. Use environment variables on other systems.' }
. (Join-Path $PSScriptRoot 'Infrastructure\VisitorCatalogConfig.ps1')
$existing = Read-VisitorCatalogConfig -Path $ConfigPath
$saved = [ordered]@{}
$labels = @{
    VisitorEmail = 'cocobuddy1216@gmail.com'
    ExperienceTitle = 'Guided Snorkeling Adventure at Pigeon Island'
    ExperienceDate = '2026-11-11'
    RestaurantTitle = 'Guided Snorkeling Adventure at Pigeon Island'
    RestaurantDate = '2026-10-19'
    AccommodationTitle = 'Mountain View Deluxe Room'
    AccommodationDate = '2026-10-29'
    CheckOut = '2026-10-31'
}
foreach ($entry in $VisitorCatalogSettingMap.GetEnumerator()) {
    $value = [Environment]::GetEnvironmentVariable($entry.Value, 'Process')
    if ([string]::IsNullOrWhiteSpace($value)) { $value = Get-VisitorCatalogConfigValue -Settings $existing -Name $entry.Key }
    if ([string]::IsNullOrWhiteSpace($value)) {
        $value = switch ($entry.Key) {
            'BaseUrl' { 'https://jolly-field-0aaea8a00.7.azurestaticapps.net' }
            'Guests' { '1' }
            default { Read-Host $labels[$entry.Key] }
        }
    }
    if ([string]::IsNullOrWhiteSpace($value)) { throw "A value is required for $($entry.Key). Settings were not saved." }
    $saved[$entry.Key] = $value
}

$encrypted = Get-VisitorCatalogConfigValue -Settings $existing -Name 'VisitorPasswordEncrypted'
if ($ResetPassword -or [string]::IsNullOrWhiteSpace($encrypted)) {
    $passwordFromEnvironment = [Environment]::GetEnvironmentVariable('CQ_E2E_VISITOR_PASSWORD', 'Process')
    $securePassword = if (-not $ResetPassword -and -not [string]::IsNullOrWhiteSpace($passwordFromEnvironment)) {
        ConvertTo-SecureString -String $passwordFromEnvironment -AsPlainText -Force
    }
    else { Read-Host 'Valid visitor password (saved encrypted for this Windows account)' -AsSecureString }
    try {
        if ($securePassword.Length -eq 0) { throw 'Visitor password is required. Settings were not saved.' }
        try { $encrypted = ConvertFrom-SecureString -SecureString $securePassword -ErrorAction Stop }
        catch { throw 'Windows could not encrypt the password. Run this script from a normal PowerShell terminal under your Windows account. No settings were saved.' }
    }
    finally {
        if ($null -ne $securePassword) { $securePassword.Dispose() }
        $passwordFromEnvironment = $null
    }
}
$saved['VisitorPasswordEncrypted'] = $encrypted
$resolvedConfigPath = [IO.Path]::GetFullPath($ConfigPath)
$configDirectory = Split-Path -Parent $resolvedConfigPath
New-Item -ItemType Directory -Path $configDirectory -Force | Out-Null
$saved | ConvertTo-Json | Set-Content -LiteralPath $resolvedConfigPath -Encoding UTF8
Write-Host "Visitor settings saved to $resolvedConfigPath"
Write-Host 'The runner loads this file automatically. Dates and listing details can be edited in the JSON file.'
Write-Host 'The password is encrypted for this Windows account on this computer.'

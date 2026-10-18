[CmdletBinding()]
param(
    [ValidateSet('All', 'Browse', 'Details', 'Availability')][string]$Area = 'All',
    [ValidatePattern('^VC_(00[1-9]|01[0-8])$')][string]$CaseId,
    [string]$BaseUrl,
    [string]$ConfigPath = (Join-Path $PSScriptRoot 'TestResults\visitor-settings.local.json'),
    [switch]$ShowBrowser,
    [switch]$ListTests
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Infrastructure\VisitorCatalogConfig.ps1')
if ($CaseId -in @('VC_009', 'VC_010', 'VC_013')) {
    throw "$CaseId was removed from this suite at the user's request. See Visitor/docs/test-cases.md."
}
if ($PSBoundParameters.ContainsKey('ConfigPath') -and -not (Test-Path -LiteralPath $ConfigPath -PathType Leaf)) {
    throw "Visitor settings file not found: $ConfigPath"
}
$project = Join-Path $PSScriptRoot '..\CeylonQuest.E2E.Tests.csproj'
$results = Join-Path $PSScriptRoot 'TestResults'
$scope = 'FullyQualifiedName~CeylonQuest.E2E.Tests.Visitor.Tests.'
$areaFilter = switch ($Area) {
    'Browse' { 'FullyQualifiedName~VisitorCatalogBrowsingTests' }
    'Details' { 'FullyQualifiedName~VisitorCatalogDetailsTests' }
    'Availability' { 'FullyQualifiedName~VisitorCatalogAvailabilityTests' }
}
if ($CaseId -and $Area -ne 'All') {
    throw 'Use -CaseId or -Area independently to avoid selecting zero tests.'
}
$testFilter = "($scope&Category=VisitorCatalog)"
if ($areaFilter) { $testFilter = "($testFilter&$areaFilter)" }
if ($CaseId) { $testFilter = "($testFilter&CaseId=$CaseId)" }

$originalEnvironment = @{}
$environmentNames = @($VisitorCatalogSettingMap.Values) + @('CQ_E2E_VISITOR_PASSWORD', 'CQ_E2E_HEADLESS', 'CQ_E2E_EVIDENCE_DIR')
foreach ($name in $environmentNames) {
    $originalEnvironment[$name] = [Environment]::GetEnvironmentVariable($name, 'Process')
}
try {
    if (-not $ListTests) {
        $settings = Read-VisitorCatalogConfig -Path $ConfigPath
        foreach ($entry in $VisitorCatalogSettingMap.GetEnumerator()) {
            if (-not [string]::IsNullOrWhiteSpace([Environment]::GetEnvironmentVariable($entry.Value, 'Process'))) { continue }
            $value = Get-VisitorCatalogConfigValue -Settings $settings -Name $entry.Key
            if (-not [string]::IsNullOrWhiteSpace($value)) { [Environment]::SetEnvironmentVariable($entry.Value, $value, 'Process') }
        }
        if ([string]::IsNullOrWhiteSpace($env:CQ_E2E_VISITOR_PASSWORD)) {
            $encrypted = Get-VisitorCatalogConfigValue -Settings $settings -Name 'VisitorPasswordEncrypted'
            if (-not [string]::IsNullOrWhiteSpace($encrypted)) {
                if ($env:OS -ne 'Windows_NT') { throw 'Saved password requires the Windows account/computer that created it.' }
                $securePassword = $null
                try {
                    $securePassword = ConvertTo-SecureString -String $encrypted -ErrorAction Stop
                    $env:CQ_E2E_VISITOR_PASSWORD = [System.Net.NetworkCredential]::new('', $securePassword).Password
                }
                catch { throw 'Cannot decrypt saved visitor password. Run Save-VisitorCatalogSettings.ps1 -ResetPassword on this Windows account.' }
                finally { if ($null -ne $securePassword) { $securePassword.Dispose() } }
            }
        }
    }
    if (-not $ListTests) {
        $required = @('CQ_E2E_VISITOR_EMAIL', 'CQ_E2E_VISITOR_PASSWORD')
        $types = if ($CaseId -in @('VC_015', 'VC_018')) { @('EXPERIENCE') }
            elseif ($CaseId -eq 'VC_016') { @('RESTAURANT') }
            elseif ($CaseId -eq 'VC_017') { @('ACCOMMODATION') }
            elseif (-not $CaseId -and $Area -in @('All', 'Availability')) { @('EXPERIENCE', 'RESTAURANT', 'ACCOMMODATION') }
            else { @() }
        foreach ($type in $types) {
            $required += "CQ_E2E_CATALOG_${type}_TITLE"
            $required += "CQ_E2E_CATALOG_${type}_DATE"
        }
        if ($types -contains 'ACCOMMODATION') { $required += 'CQ_E2E_CATALOG_CHECK_OUT' }
        $missing = @($required | Where-Object { [string]::IsNullOrWhiteSpace([Environment]::GetEnvironmentVariable($_)) })
        if ($missing.Count -gt 0) {
            throw "Missing configuration: $($missing -join ', '). Run Visitor\Save-VisitorCatalogSettings.ps1 once, or set these environment variables. No browser tests were run."
        }
    }

    if ($BaseUrl) { $env:CQ_E2E_BASE_URL = $BaseUrl }
    if ($ShowBrowser) { $env:CQ_E2E_HEADLESS = 'false' }
    $env:CQ_E2E_EVIDENCE_DIR = Join-Path $results 'screenshots'
    Write-Host "Visitor catalog tests: $Area; filter: $testFilter"
    $testArgs = @('test', $project, '--filter', $testFilter, '--nologo')
    if ($ListTests) { $testArgs += '--list-tests' }
    else {
        $testArgs += @('--logger', 'trx;LogFileName=visitor-catalog-results.trx', '--logger', 'console;verbosity=normal',
            '--results-directory', $results)
    }
    & dotnet @testArgs
    if ($LASTEXITCODE -ne 0) { throw "Visitor catalog tests returned exit code $LASTEXITCODE. Inspect $results." }
}
finally {
    foreach ($name in $environmentNames) {
        [Environment]::SetEnvironmentVariable($name, $originalEnvironment[$name], 'Process')
    }
}

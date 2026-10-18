[CmdletBinding()]
param(
    [ValidatePattern('^AC_(00[1-9]|01[0-7])$')][string]$CaseId,
    [string]$BaseUrl,
    [switch]$ShowBrowser,
    [switch]$ListTests
)

$ErrorActionPreference = 'Stop'
$project = Join-Path $PSScriptRoot '..\CeylonQuest.E2E.Tests.csproj'
$results = Join-Path $PSScriptRoot 'TestResults'
$filter = '(FullyQualifiedName~CeylonQuest.E2E.Tests.Admin.Tests.&Category=AdminCatalog)'
if ($CaseId) { $filter = "($filter&CaseId=$CaseId)" }

if (-not $ListTests) {
    $required = @('CQ_E2E_ADMIN_EMAIL', 'CQ_E2E_ADMIN_PASSWORD')
    $missing = @($required | Where-Object { [string]::IsNullOrWhiteSpace([Environment]::GetEnvironmentVariable($_, 'Process')) })
    if ($missing.Count -gt 0) {
        throw "Missing Admin configuration: $($missing -join ', '). Enter terminal environment variables using Admin\README.md. No browser tests were run."
    }
    foreach ($name in @('CQ_E2E_ADMIN_CATALOG_APPLICATION_ID', 'CQ_E2E_ADMIN_CATALOG_PENDING_APPLICATION_ID',
        'CQ_E2E_ADMIN_CATALOG_DOCUMENT_APPLICATION_ID', 'CQ_E2E_ADMIN_CATALOG_LISTING_ID')) {
        $value = [Environment]::GetEnvironmentVariable($name, 'Process')
        if ([string]::IsNullOrWhiteSpace($value)) { continue }
        $parsedId = [Guid]::Empty
        if (-not [Guid]::TryParse($value, [ref]$parsedId) -or $parsedId -eq [Guid]::Empty) {
            throw "$name must be an existing catalog GUID, or leave it unset for automatic selection."
        }
    }
    if ($env:CQ_E2E_ADMIN_CATALOG_LISTING_TYPE -and $env:CQ_E2E_ADMIN_CATALOG_LISTING_TYPE -notin @('Experience','Restaurant','Accommodation')) {
        throw 'CQ_E2E_ADMIN_CATALOG_LISTING_TYPE must be Experience, Restaurant or Accommodation.'
    }
    if ($env:CQ_E2E_ADMIN_CATALOG_DOCUMENT_INDEX) {
        $documentIndex = 0
        if (-not [int]::TryParse($env:CQ_E2E_ADMIN_CATALOG_DOCUMENT_INDEX, [ref]$documentIndex) -or $documentIndex -lt 0) {
            throw 'CQ_E2E_ADMIN_CATALOG_DOCUMENT_INDEX must be a nonnegative integer.'
        }
    }
}

$originalBase = $env:CQ_E2E_BASE_URL
$originalHeadless = $env:CQ_E2E_HEADLESS
try {
    if ($BaseUrl) { $env:CQ_E2E_BASE_URL = $BaseUrl }
    if ($ShowBrowser) { $env:CQ_E2E_HEADLESS = 'false' }
    Write-Host "Admin Catalog tests (screenshots disabled); filter: $filter"
    $testArguments = @('test', $project, '--filter', $filter, '--nologo')
    if ($ListTests) { $testArguments += '--list-tests' }
    else {
        $testArguments += @('--logger', 'trx;LogFileName=admin-catalog-results.trx',
            '--logger', 'console;verbosity=normal', '--results-directory', $results)
    }
    & dotnet @testArguments
    if ($LASTEXITCODE -ne 0) { throw "Admin Catalog tests returned exit code $LASTEXITCODE. Inspect $results\admin-catalog-results.trx and console output." }
}
finally {
    $env:CQ_E2E_BASE_URL = $originalBase
    $env:CQ_E2E_HEADLESS = $originalHeadless
}

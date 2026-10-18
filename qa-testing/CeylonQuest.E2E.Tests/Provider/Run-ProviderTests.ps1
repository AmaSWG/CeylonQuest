[CmdletBinding()]
param(
    [ValidatePattern('^PC_(00[1-9]|010|011)$')][string]$CaseId,
    [string]$BaseUrl,
    [switch]$ShowBrowser,
    [switch]$ListTests
)

$ErrorActionPreference = 'Stop'
$project = Join-Path $PSScriptRoot '..\CeylonQuest.E2E.Tests.csproj'
$results = Join-Path $PSScriptRoot 'TestResults'
$testFilter = '(FullyQualifiedName~CeylonQuest.E2E.Tests.Provider.Tests.&Category=ProviderCatalog)'
if ($CaseId) { $testFilter = "($testFilter&CaseId=$CaseId)" }

# Environment variables are supplied by the user in this terminal; no credential file.
if (-not $ListTests) {
    $required = @('CQ_E2E_PROVIDER_EMAIL', 'CQ_E2E_PROVIDER_PASSWORD')
    $missing = @($required | Where-Object { [string]::IsNullOrWhiteSpace([Environment]::GetEnvironmentVariable($_, 'Process')) })
    if ($missing.Count -gt 0) {
        throw "Missing Provider configuration: $($missing -join ', '). Set these in this terminal using Provider\README.md. No browser tests were run."
    }
    if (-not [string]::IsNullOrWhiteSpace($env:CQ_E2E_PROVIDER_LISTING_ID)) {
        $listingGuid = [Guid]::Empty
        if (-not [Guid]::TryParse($env:CQ_E2E_PROVIDER_LISTING_ID, [ref]$listingGuid) -or $listingGuid -eq [Guid]::Empty) {
            throw 'CQ_E2E_PROVIDER_LISTING_ID must be an existing owned listing GUID, or leave it unset for automatic selection.'
        }
    }
}
$originalBase = $env:CQ_E2E_BASE_URL
$originalHeadless = $env:CQ_E2E_HEADLESS
$originalEvidence = $env:CQ_E2E_EVIDENCE_DIR
try {
    if ($BaseUrl) { $env:CQ_E2E_BASE_URL = $BaseUrl }
    if ($ShowBrowser) { $env:CQ_E2E_HEADLESS = 'false' }
    $env:CQ_E2E_EVIDENCE_DIR = Join-Path $results 'screenshots'
    Write-Host "Provider Catalog tests; filter: $testFilter"
    $testArguments = @('test', $project, '--filter', $testFilter, '--nologo')
    if ($ListTests) { $testArguments += '--list-tests' }
    else {
        $testArguments += @('--logger', 'trx;LogFileName=provider-catalog-results.trx',
            '--logger', 'console;verbosity=normal', '--results-directory', $results)
    }
    & dotnet @testArguments
    if ($LASTEXITCODE -ne 0) { throw "Provider Catalog tests returned exit code $LASTEXITCODE. Inspect $results." }
}
finally {
    $env:CQ_E2E_BASE_URL = $originalBase
    $env:CQ_E2E_HEADLESS = $originalHeadless
    $env:CQ_E2E_EVIDENCE_DIR = $originalEvidence
}

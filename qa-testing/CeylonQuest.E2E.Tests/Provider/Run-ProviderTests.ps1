param(
    [ValidateSet('ReadOnly', 'All', 'Writes')]
    [string]$Mode = 'ReadOnly',
    [ValidatePattern('^PROV-[A-Z0-9]+-[0-9]{2}$')]
    [string]$CaseId,
    [string]$BaseUrl,
    [switch]$ShowBrowser
)

# Run this script normally with &, not dot-sourced. Credentials are prompted for
# locally, passed only to the test child process, and restored/removed afterwards.
$ErrorActionPreference = 'Stop'
$projectPath = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\CeylonQuest.E2E.Tests.csproj'))
$resultsPath = Join-Path $PSScriptRoot 'TestResults'
$savedEnvironment = @{}
foreach ($variableName in @('CQ_E2E_PROVIDER_EMAIL', 'CQ_E2E_PROVIDER_PASSWORD', 'CQ_E2E_BASE_URL', 'CQ_E2E_HEADLESS', 'CQ_E2E_EVIDENCE_DIR')) {
    $savedEnvironment[$variableName] = [Environment]::GetEnvironmentVariable($variableName, 'Process')
}

try {
    if ([string]::IsNullOrWhiteSpace($env:CQ_E2E_PROVIDER_EMAIL)) {
        $env:CQ_E2E_PROVIDER_EMAIL = Read-Host 'Enter the provider email address'
    }
    if ([string]::IsNullOrWhiteSpace($env:CQ_E2E_PROVIDER_PASSWORD)) {
        $providerTestPassword = Read-Host 'Enter the provider password (hidden)' -AsSecureString
        try {
            $env:CQ_E2E_PROVIDER_PASSWORD = [System.Net.NetworkCredential]::new('', $providerTestPassword).Password
        }
        finally { $providerTestPassword.Dispose() }
    }
    if ($BaseUrl) { $env:CQ_E2E_BASE_URL = $BaseUrl }
    $env:CQ_E2E_HEADLESS = if ($ShowBrowser) { 'false' } else { 'true' }
    $env:CQ_E2E_EVIDENCE_DIR = Join-Path $resultsPath 'screenshots'

    $providerFilter = 'Workflow=Provider'
    if ($Mode -eq 'ReadOnly') { $providerFilter += '&Safety=ReadOnly' }
    if ($Mode -eq 'Writes') { $providerFilter += '&Safety=Writes' }
    if ($CaseId) { $providerFilter += "&CaseId=$CaseId" }

    # This launcher never enables mutation opt-ins, changes Stripe settings,
    # registers users, seeds records, or writes a credentials file.
    & dotnet test $projectPath --filter $providerFilter `
        --logger 'trx;LogFileName=provider-results.trx' `
        --logger 'console;verbosity=detailed' `
        --results-directory $resultsPath
    $providerExitCode = $LASTEXITCODE
}
finally {
    foreach ($variableName in $savedEnvironment.Keys) {
        [Environment]::SetEnvironmentVariable($variableName, $savedEnvironment[$variableName], 'Process')
    }
}

if ($providerExitCode -ne 0) {
    throw "Provider tests returned exit code $providerExitCode. Inspect the output and $resultsPath\provider-results.trx."
}

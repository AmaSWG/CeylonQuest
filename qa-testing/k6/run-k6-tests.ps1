<#
.SYNOPSIS
    CeylonQuest k6 Performance Test Runner
    Executes load & concurrency performance tests using Grafana k6.

.PARAMETER Target
    Execution environment: 'Local' (default) or 'AzureCloud'

.PARAMETER Test
    Which test suite to run: 'Bookings' (default), 'Notifications', or 'All'
#>

param(
    [ValidateSet("Local", "AzureCloud")]
    [string]$Target = "Local",

    [ValidateSet("Bookings", "Notifications", "Identity", "Catalog", "All")]
    [string]$Test = "Bookings",

    [int]$SlaMs = 2000,

    [switch]$OpenReport
)

$ErrorActionPreference = "Continue"

# 1. Resolve k6 Executable Path
$k6Cmd = Get-Command k6 -ErrorAction SilentlyContinue
if ($null -eq $k6Cmd) {
    if (Test-Path "C:\Program Files\k6\k6.exe") {
        $k6Exe = "C:\Program Files\k6\k6.exe"
    } elseif (Test-Path "$env:LOCALAPPDATA\Programs\k6\k6.exe") {
        $k6Exe = "$env:LOCALAPPDATA\Programs\k6\k6.exe"
    } else {
        Write-Error "k6 executable not found. Please install k6 or restart your PowerShell terminal."
        exit 1
    }
} else {
    $k6Exe = $k6Cmd.Source
}

Write-Host "=======================================================" -ForegroundColor Cyan
Write-Host " CeylonQuest k6 Performance & Load Testing Runner     " -ForegroundColor Cyan
Write-Host " k6 Binary : $k6Exe" -ForegroundColor Cyan
Write-Host " Target Env: $Target | Test Suite: $Test" -ForegroundColor Cyan
Write-Host "=======================================================" -ForegroundColor Cyan

# 2. Configure Endpoints based on Target
if ($Target -eq "AzureCloud") {
    $BookingUrl = "https://ceylonquest-booking-service-gccuc7fbhyckcpg9.southeastasia-01.azurewebsites.net"
    $IdentityUrl = "https://ceylonquest-identity-fphphwe7cfbtadhu.southeastasia-01.azurewebsites.net"
    $NotifUrl = "https://jolly-field-0aaea8a00.7.azurestaticapps.net"
    $CatalogUrl = "https://ceylonquest-provider-catalog-fecca6csaubwa2em.southeastasia-01.azurewebsites.net"
} else {
    $BookingUrl = "http://localhost:5229"
    $IdentityUrl = "http://localhost:5278"
    $NotifUrl = "http://localhost:5278"
    $CatalogUrl = "http://localhost:5141"
}

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ReportsDir = Join-Path $ScriptDir "reports"
if (-not (Test-Path $ReportsDir)) {
    New-Item -ItemType Directory -Path $ReportsDir -Force | Out-Null
}

# 3. Execute Tests
function Run-Suite($testName, $scriptPath, $targetUrl, $token = "") {
    Write-Host "`n>>> Running $testName against $targetUrl ..." -ForegroundColor Yellow
    $summaryFile = Join-Path $ReportsDir "$testName-summary.json"
    
    $env:TARGET_URL = $targetUrl
    $env:AUTH_TOKEN = $token
    $env:SLA_MS = "$SlaMs"

    & $k6Exe run -e TARGET_URL="$targetUrl" -e AUTH_TOKEN="$token" -e SLA_MS="$SlaMs" --summary-export $summaryFile $scriptPath
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host ">>> [PASS] $testName passed all performance thresholds!" -ForegroundColor Green
    } else {
        Write-Host ">>> [FAIL] $testName threshold failed (Exit code: $LASTEXITCODE)" -ForegroundColor Red
    }
}

if ($Test -eq "Bookings" -or $Test -eq "All") {
    $bookingScript = Join-Path $ScriptDir "test-bookings.js"
    Run-Suite -testName "Bookings" -scriptPath $bookingScript -targetUrl $BookingUrl
}

if ($Test -eq "Notifications" -or $Test -eq "All") {
    $notifScript = Join-Path $ScriptDir "test-notifications.js"
    $loginBody = @{ email = "piyumiyashodara@gmail.com"; password = "Piyumi@2003" } | ConvertTo-Json
    $token = ""
    try {
        $loginRes = Invoke-RestMethod -Uri "$IdentityUrl/api/auth/login" -Method Post -Body $loginBody -ContentType "application/json" -TimeoutSec 10 -ErrorAction SilentlyContinue
        $token = $loginRes.token
        Write-Host "Authenticated successfully as $($loginRes.email)" -ForegroundColor Green
    } catch {
        Write-Warning "Could not authenticate with Identity Service. Proceeding with anonymous/public requests."
    }

    Run-Suite -testName "Notifications" -scriptPath $notifScript -targetUrl $NotifUrl -token $token
}

if ($Test -eq "Identity" -or $Test -eq "All") {
    $identityScript = Join-Path $ScriptDir "test-identity.js"
    Run-Suite -testName "Identity" -scriptPath $identityScript -targetUrl $IdentityUrl
}

if ($Test -eq "Catalog" -or $Test -eq "All") {
    $catalogScript = Join-Path $ScriptDir "test-catalog.js"
    Run-Suite -testName "Catalog" -scriptPath $catalogScript -targetUrl $CatalogUrl
}

Write-Host "`n=======================================================" -ForegroundColor Cyan
Write-Host " k6 Test Execution Finished. Reports saved in: $ReportsDir" -ForegroundColor Cyan
Write-Host "=======================================================" -ForegroundColor Cyan

if ($OpenReport) {
    $reportToOpen = ""
    switch ($Test) {
        "Bookings"      { $reportToOpen = Join-Path $ReportsDir "bookings-report.html" }
        "Notifications" { $reportToOpen = Join-Path $ReportsDir "notifications-report.html" }
        "Identity"      { $reportToOpen = Join-Path $ReportsDir "identity-report.html" }
        "Catalog"       { $reportToOpen = Join-Path $ReportsDir "catalog-report.html" }
        default         { $reportToOpen = Join-Path $ReportsDir "bookings-report.html" }
    }

    if (Test-Path $reportToOpen) {
        Write-Host "Launching report in browser: $reportToOpen" -ForegroundColor Green
        Start-Process $reportToOpen
    }
}

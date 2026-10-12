$rootDir = $PSScriptRoot
$projectPath = Join-Path $rootDir "services\booking-service\BookingService.Tests\BookingService.Tests.csproj"
$resultsDir = Join-Path $rootDir "services\booking-service\BookingService.Tests\TestResults"
$reportDir = Join-Path $rootDir "services\booking-service\BookingService.Tests\coverage-report"
$runSettings = Join-Path $rootDir "services\booking-service\BookingService.Tests\coverage.runsettings"

Write-Host "Running Story 12.1 Test Suite & Collecting Code Coverage..." -ForegroundColor Cyan

# 1. Run tests with coverage collection
dotnet test $projectPath --no-build --filter "FullyQualifiedName~ReviewTests" --collect:"XPlat Code Coverage" --settings $runSettings --verbosity quiet

# 2. Locate latest coverage file
$coverageFiles = Get-ChildItem -Recurse $resultsDir -Filter "coverage.cobertura.xml" -ErrorAction SilentlyContinue | Sort-Object LastWriteTime -Descending
if (-not $coverageFiles) {
    Write-Error "No coverage file found. Please ensure tests ran successfully."
    exit 1
}

$latest = $coverageFiles[0].FullName

# 3. Generate HTML & Summary.txt via ReportGenerator
reportgenerator -reports:$latest -targetdir:$reportDir -assemblyfilters:"+booking-service" -classfilters:"+*Review*;-*Migrations*;-*MigrationContextFactory*" -reporttypes:"Html;TextSummary" | Out-Null

# 4. Display Summary report
$summaryPath = Join-Path $reportDir "Summary.txt"
if (Test-Path $summaryPath) {
    Write-Host "`n"
    Get-Content $summaryPath
    Write-Host "`nHTML Report generated at: file:///$($reportDir.Replace('\', '/'))/index.html" -ForegroundColor Green
}

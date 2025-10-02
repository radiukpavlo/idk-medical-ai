param(
    [string]$Configuration = "Debug",
    [string]$Filter = "",
    [switch]$Coverage = $false,
    [switch]$Verbose = $false,
    [string]$Logger = "console;verbosity=normal",
    [string]$OutputPath = "./test-results"
)

$ErrorActionPreference = "Stop"

Write-Host "=== MedicalAI Thesis Suite Test Runner ===" -ForegroundColor Green
Write-Host "Configuration: $Configuration" -ForegroundColor Cyan
Write-Host "Filter: $(if ($Filter) { $Filter } else { 'All tests' })" -ForegroundColor Cyan
Write-Host "Coverage: $Coverage" -ForegroundColor Cyan

# Create output directory
if (!(Test-Path $OutputPath)) {
    New-Item -ItemType Directory -Path $OutputPath -Force | Out-Null
}

# Clean and restore
Write-Host "Preparing test environment..." -ForegroundColor Yellow
dotnet clean --configuration $Configuration --verbosity minimal
dotnet restore --verbosity minimal

# Build solution
Write-Host "Building solution..." -ForegroundColor Yellow
dotnet build --configuration $Configuration --no-restore --verbosity minimal
if ($LASTEXITCODE -ne 0) {
    Write-Error "Build failed"
    exit 1
}

# Prepare test arguments
$testArgs = @(
    "test"
    "--configuration", $Configuration
    "--no-build"
    "--logger", $Logger
    "--results-directory", $OutputPath
)

if ($Filter) {
    $testArgs += "--filter", $Filter
}

if ($Verbose) {
    $testArgs += "--verbosity", "detailed"
} else {
    $testArgs += "--verbosity", "normal"
}

# Add coverage if requested
if ($Coverage) {
    Write-Host "Enabling code coverage..." -ForegroundColor Yellow
    $testArgs += "--collect", "XPlat Code Coverage"
    $testArgs += "--settings", "build/coverlet.runsettings"
}

# Run tests
Write-Host "Running tests..." -ForegroundColor Yellow
$testStartTime = Get-Date

& dotnet @testArgs

$testEndTime = Get-Date
$testDuration = $testEndTime - $testStartTime

if ($LASTEXITCODE -eq 0) {
    Write-Host "✓ All tests passed!" -ForegroundColor Green
} else {
    Write-Host "✗ Some tests failed" -ForegroundColor Red
}

Write-Host "Test duration: $($testDuration.ToString('mm\:ss'))" -ForegroundColor Cyan

# Process coverage if enabled
if ($Coverage -and $LASTEXITCODE -eq 0) {
    Write-Host "Processing coverage results..." -ForegroundColor Yellow
    
    # Find coverage files
    $coverageFiles = Get-ChildItem -Path $OutputPath -Recurse -Filter "coverage.cobertura.xml"
    
    if ($coverageFiles.Count -gt 0) {
        Write-Host "Coverage files found: $($coverageFiles.Count)" -ForegroundColor Cyan
        
        # Try to generate HTML report if reportgenerator is available
        if (Get-Command reportgenerator -ErrorAction SilentlyContinue) {
            $htmlOutputPath = Join-Path $OutputPath "coverage-html"
            reportgenerator -reports:"$($coverageFiles[0].FullName)" -targetdir:"$htmlOutputPath" -reporttypes:Html
            Write-Host "HTML coverage report: $htmlOutputPath/index.html" -ForegroundColor Cyan
        } else {
            Write-Host "Install ReportGenerator for HTML coverage reports: dotnet tool install -g dotnet-reportgenerator-globaltool" -ForegroundColor Yellow
        }
    }
}

Write-Host "Test results saved to: $OutputPath" -ForegroundColor Cyan

exit $LASTEXITCODE
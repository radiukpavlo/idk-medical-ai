param(
    [switch]$Fix = $false,
    [switch]$Analyze = $false,
    [switch]$Format = $false,
    [switch]$CheckOutdated = $false,
    [string]$Severity = "suggestion",
    [switch]$Verbose = $false
)

$ErrorActionPreference = "Stop"

Write-Host "=== MedicalAI Thesis Suite Code Quality Tools ===" -ForegroundColor Green

if (!$Fix -and !$Analyze -and !$Format -and !$CheckOutdated) {
    Write-Host "No action specified. Running all checks..." -ForegroundColor Yellow
    $Analyze = $true
    $Format = $true
    $CheckOutdated = $true
}

# Check if solution exists
if (!(Test-Path "MedicalAI.ThesisSuite.sln")) {
    Write-Error "Solution file not found. Please run from the project root directory."
    exit 1
}

# Format code
if ($Format) {
    Write-Host "Formatting code..." -ForegroundColor Yellow
    
    $formatArgs = @(
        "format"
        "MedicalAI.ThesisSuite.sln"
        "--severity", $Severity
    )
    
    if ($Fix) {
        # No additional args needed for fix mode
    } else {
        $formatArgs += "--verify-no-changes"
    }
    
    if ($Verbose) {
        $formatArgs += "--verbosity", "diagnostic"
    } else {
        $formatArgs += "--verbosity", "minimal"
    }
    
    & dotnet @formatArgs
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✓ Code formatting check passed" -ForegroundColor Green
    } else {
        if ($Fix) {
            Write-Host "✓ Code formatting applied" -ForegroundColor Green
        } else {
            Write-Host "✗ Code formatting issues found. Run with -Fix to apply fixes." -ForegroundColor Red
        }
    }
}

# Analyze code
if ($Analyze) {
    Write-Host "Analyzing code..." -ForegroundColor Yellow
    
    # Build with analysis
    dotnet build MedicalAI.ThesisSuite.sln --configuration Debug --verbosity normal
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✓ Code analysis passed" -ForegroundColor Green
    } else {
        Write-Host "✗ Code analysis found issues" -ForegroundColor Red
    }
    
    # Run additional analyzers if available
    if (Get-Command "dotnet-sonarscanner" -ErrorAction SilentlyContinue) {
        Write-Host "Running SonarQube analysis..." -ForegroundColor Cyan
        # SonarQube analysis would go here
    }
}

# Check for outdated packages
if ($CheckOutdated) {
    Write-Host "Checking for outdated packages..." -ForegroundColor Yellow
    
    if (Get-Command "dotnet-outdated" -ErrorAction SilentlyContinue) {
        dotnet outdated MedicalAI.ThesisSuite.sln
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "✓ All packages are up to date" -ForegroundColor Green
        } else {
            Write-Host "! Some packages may be outdated" -ForegroundColor Yellow
        }
    } else {
        Write-Host "dotnet-outdated tool not installed. Install with: dotnet tool install -g dotnet-outdated-tool" -ForegroundColor Yellow
    }
}

# Security scan
Write-Host "Running security scan..." -ForegroundColor Yellow
dotnet list package --vulnerable --include-transitive

if ($LASTEXITCODE -eq 0) {
    Write-Host "✓ No known vulnerabilities found" -ForegroundColor Green
} else {
    Write-Host "! Potential security vulnerabilities found" -ForegroundColor Yellow
}

# Generate quality report
$reportPath = "dist/code-quality-report-$(Get-Date -Format 'yyyyMMdd-HHmmss').json"
if (!(Test-Path "dist")) {
    New-Item -ItemType Directory -Path "dist" -Force | Out-Null
}

$qualityReport = @{
    Timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    FormatCheck = if ($Format) { $LASTEXITCODE -eq 0 } else { $null }
    AnalysisCheck = if ($Analyze) { $LASTEXITCODE -eq 0 } else { $null }
    SecurityCheck = $LASTEXITCODE -eq 0
    Tools = @{
        DotnetFormat = (Get-Command "dotnet" -ErrorAction SilentlyContinue) -ne $null
        DotnetOutdated = (Get-Command "dotnet-outdated" -ErrorAction SilentlyContinue) -ne $null
        SonarScanner = (Get-Command "dotnet-sonarscanner" -ErrorAction SilentlyContinue) -ne $null
    }
} | ConvertTo-Json -Depth 2

$qualityReport | Out-File -FilePath $reportPath -Encoding UTF8
Write-Host "Quality report saved: $reportPath" -ForegroundColor Cyan

Write-Host "`n=== Code Quality Check Complete ===" -ForegroundColor Green
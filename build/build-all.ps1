param(
    [string]$Configuration = "Release",
    [switch]$RunTests = $false,
    [switch]$Verbose = $false,
    [string]$OutputPath = "./dist"
)

$ErrorActionPreference = "Stop"

# Supported runtimes
$runtimes = @(
    "win-x64",
    "win-x86", 
    "win-arm64",
    "linux-x64",
    "linux-arm64",
    "osx-x64",
    "osx-arm64"
)

Write-Host "=== Building MedicalAI Thesis Suite for All Platforms ===" -ForegroundColor Green
Write-Host "Configuration: $Configuration" -ForegroundColor Cyan
Write-Host "Platforms: $($runtimes -join ', ')" -ForegroundColor Cyan

$buildResults = @()
$startTime = Get-Date

foreach ($runtime in $runtimes) {
    Write-Host "`n--- Building for $runtime ---" -ForegroundColor Yellow
    $runtimeStartTime = Get-Date
    
    try {
        $params = @{
            Configuration = $Configuration
            Runtime = $runtime
            OutputPath = $OutputPath
            SkipRestore = $true  # Only restore once
        }
        
        if ($RunTests -and $runtime -eq "win-x64") {
            # Only run tests once on the primary platform
            $params.RunTests = $true
        }
        
        if ($Verbose) {
            $params.Verbose = $true
        }
        
        & "$PSScriptRoot/publish.ps1" @params
        
        $buildTime = (Get-Date) - $runtimeStartTime
        $buildResults += [PSCustomObject]@{
            Runtime = $runtime
            Status = "Success"
            BuildTime = $buildTime.ToString("mm\:ss")
            Error = $null
        }
        
        Write-Host "✓ $runtime completed in $($buildTime.ToString('mm\:ss'))" -ForegroundColor Green
    }
    catch {
        $buildTime = (Get-Date) - $runtimeStartTime
        $buildResults += [PSCustomObject]@{
            Runtime = $runtime
            Status = "Failed"
            BuildTime = $buildTime.ToString("mm\:ss")
            Error = $_.Exception.Message
        }
        
        Write-Host "✗ $runtime failed: $($_.Exception.Message)" -ForegroundColor Red
    }
}

$totalTime = (Get-Date) - $startTime

Write-Host "`n=== Build Summary ===" -ForegroundColor Green
$buildResults | Format-Table -AutoSize

$successCount = ($buildResults | Where-Object { $_.Status -eq "Success" }).Count
$failCount = ($buildResults | Where-Object { $_.Status -eq "Failed" }).Count

Write-Host "Total builds: $($buildResults.Count)" -ForegroundColor Cyan
Write-Host "Successful: $successCount" -ForegroundColor Green
Write-Host "Failed: $failCount" -ForegroundColor Red
Write-Host "Total time: $($totalTime.ToString('hh\:mm\:ss'))" -ForegroundColor Cyan

# Generate build report
$reportPath = Join-Path $OutputPath "build-report-$(Get-Date -Format 'yyyyMMdd-HHmmss').json"
$buildReport = @{
    BuildDate = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    Configuration = $Configuration
    TotalTime = $totalTime.ToString()
    Results = $buildResults
    Summary = @{
        Total = $buildResults.Count
        Successful = $successCount
        Failed = $failCount
    }
} | ConvertTo-Json -Depth 3

$buildReport | Out-File -FilePath $reportPath -Encoding UTF8
Write-Host "Build report saved: $reportPath" -ForegroundColor Cyan

if ($failCount -gt 0) {
    exit 1
}
param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [switch]$RunTests = $false,
    [switch]$SkipRestore = $false,
    [switch]$Verbose = $false,
    [string]$OutputPath = "./dist"
)

$ErrorActionPreference = "Stop"

# Set verbose preference
if ($Verbose) {
    $VerbosePreference = "Continue"
}

Write-Host "=== MedicalAI Thesis Suite Build Script ===" -ForegroundColor Green
Write-Host "Configuration: $Configuration" -ForegroundColor Cyan
Write-Host "Runtime: $Runtime" -ForegroundColor Cyan
Write-Host "Run Tests: $RunTests" -ForegroundColor Cyan
Write-Host "Output Path: $OutputPath" -ForegroundColor Cyan

# Create output directory
if (!(Test-Path $OutputPath)) {
    New-Item -ItemType Directory -Path $OutputPath -Force | Out-Null
    Write-Host "Created output directory: $OutputPath" -ForegroundColor Yellow
}

# Clean previous builds
Write-Host "Cleaning previous builds..." -ForegroundColor Yellow
Get-ChildItem -Path "src" -Recurse -Directory -Name "bin", "obj" | ForEach-Object {
    $fullPath = Join-Path "src" $_
    if (Test-Path $fullPath) {
        Remove-Item $fullPath -Recurse -Force
        Write-Verbose "Cleaned: $fullPath"
    }
}

# Restore packages
if (!$SkipRestore) {
    Write-Host "Restoring NuGet packages..." -ForegroundColor Yellow
    dotnet restore --verbosity minimal
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Package restore failed"
        exit 1
    }
}

# Build solution
Write-Host "Building solution..." -ForegroundColor Yellow
dotnet build --configuration $Configuration --no-restore --verbosity minimal
if ($LASTEXITCODE -ne 0) {
    Write-Error "Build failed"
    exit 1
}

# Run tests if requested
if ($RunTests) {
    Write-Host "Running tests..." -ForegroundColor Yellow
    dotnet test --configuration $Configuration --no-build --verbosity minimal --logger "console;verbosity=normal"
    if ($LASTEXITCODE -ne 0) {
        Write-Warning "Some tests failed, but continuing with build"
    }
}

# Publish application
Write-Host "Publishing application for $Runtime..." -ForegroundColor Yellow
$publishPath = "src/MedicalAI.UI/bin/$Configuration/net8.0/$Runtime/publish"
dotnet publish ./src/MedicalAI.UI -c $Configuration -r $Runtime -p:PublishSingleFile=true -p:SelfContained=true --no-build
if ($LASTEXITCODE -ne 0) {
    Write-Error "Publish failed"
    exit 1
}

# Copy models and datasets if they exist
$modelsPath = "models"
$datasetsPath = "datasets/samples"
if (Test-Path $modelsPath) {
    Write-Host "Copying AI models..." -ForegroundColor Yellow
    $targetModelsPath = Join-Path $publishPath "models"
    Copy-Item -Path $modelsPath -Destination $targetModelsPath -Recurse -Force
}

if (Test-Path $datasetsPath) {
    Write-Host "Copying sample datasets..." -ForegroundColor Yellow
    $targetDatasetsPath = Join-Path $publishPath "datasets"
    New-Item -ItemType Directory -Path $targetDatasetsPath -Force | Out-Null
    Copy-Item -Path "$datasetsPath/*" -Destination $targetDatasetsPath -Recurse -Force
}

# Create distribution package
$packageName = "MedicalAI.ThesisSuite-$Runtime-$(Get-Date -Format 'yyyyMMdd')"
$packagePath = Join-Path $OutputPath "$packageName.zip"

Write-Host "Creating distribution package..." -ForegroundColor Yellow
Compress-Archive -Path "$publishPath/*" -DestinationPath $packagePath -Force

# Generate build info
$buildInfo = @{
    BuildDate = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    Configuration = $Configuration
    Runtime = $Runtime
    Version = (dotnet --version)
    GitCommit = if (Get-Command git -ErrorAction SilentlyContinue) { git rev-parse --short HEAD 2>$null } else { "unknown" }
} | ConvertTo-Json -Depth 2

$buildInfoPath = Join-Path $OutputPath "$packageName-buildinfo.json"
$buildInfo | Out-File -FilePath $buildInfoPath -Encoding UTF8

Write-Host "=== Build Complete ===" -ForegroundColor Green
Write-Host "Package: $packagePath" -ForegroundColor Cyan
Write-Host "Build Info: $buildInfoPath" -ForegroundColor Cyan
Write-Host "Package Size: $([math]::Round((Get-Item $packagePath).Length / 1MB, 2)) MB" -ForegroundColor Cyan

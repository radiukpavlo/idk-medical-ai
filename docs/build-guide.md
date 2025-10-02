# Build and Run Guide

This comprehensive guide provides detailed instructions for building and running the MedicalAI Thesis Suite on all supported platforms.

## Table of Contents
- [Prerequisites](#prerequisites)
- [Windows Build Instructions](#windows-build-instructions)
- [Linux Build Instructions](#linux-build-instructions)
- [macOS Build Instructions](#macos-build-instructions)
- [Development Environment Setup](#development-environment-setup)
- [Troubleshooting](#troubleshooting)
- [Deployment and Distribution](#deployment-and-distribution)

## Prerequisites

### System Requirements
- **Minimum RAM**: 4 GB (8 GB recommended)
- **Disk Space**: 2 GB available
- **Internet Connection**: Required for initial setup and package downloads

### Required Software
- **.NET 8.0 SDK**: Latest version from [Microsoft](https://dotnet.microsoft.com/download/dotnet/8.0)
- **Git**: For source code management
- **Text Editor/IDE**: Visual Studio 2022, JetBrains Rider, or VS Code

## Windows Build Instructions

### Step 1: Install Prerequisites

#### Install .NET 8.0 SDK
**Option A: Using Windows Package Manager (Recommended)**
```powershell
# Open PowerShell as Administrator
winget install Microsoft.DotNet.SDK.8
```

**Option B: Manual Download**
1. Visit [.NET 8.0 Download Page](https://dotnet.microsoft.com/download/dotnet/8.0)
2. Download "SDK x64" for Windows
3. Run the installer and follow the setup wizard
4. Restart your command prompt/PowerShell

**Option C: Using Chocolatey**
```powershell
# If you have Chocolatey installed
choco install dotnet-8.0-sdk
```

#### Install Git
```powershell
winget install Git.Git
```

#### Verify Installation
```powershell
# Check .NET version
dotnet --version
# Should output: 8.0.x or higher

# Check Git version
git --version
```

### Step 2: Clone and Build

#### Clone the Repository
```powershell
# Navigate to your development directory
cd C:\Development  # or your preferred location

# Clone the repository
git clone <repository-url>
cd MedicalAI.ThesisSuite
```

#### Build the Project
```powershell
# Restore NuGet packages
dotnet restore

# Build in Debug mode (for development)
dotnet build --configuration Debug

# Build in Release mode (for production)
dotnet build --configuration Release
```

### Step 3: Run the Application

#### Run from Source (Development)
```powershell
# Run the UI application
dotnet run --project src/MedicalAI.UI

# Run the CLI application
dotnet run --project src/MedicalAI.CLI
```

#### Create and Run Executable
```powershell
# Create self-contained executable
dotnet publish src/MedicalAI.UI -c Release -r win-x64 -p:PublishSingleFile=true -p:SelfContained=true

# Navigate to output directory
cd src\MedicalAI.UI\bin\Release\net8.0\win-x64\publish

# Run the executable
.\MedicalAI.UI.exe
```

### Step 4: Create Distribution Package
```powershell
# Use the provided build script
.\build\publish.ps1 win-x64

# This creates MedicalAI.ThesisSuite.zip in the root directory
```

## Linux Build Instructions

### Step 1: Install Prerequisites

#### Ubuntu/Debian
```bash
# Update package list
sudo apt update

# Install required packages
sudo apt install -y wget apt-transport-https software-properties-common

# Add Microsoft package repository
wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
rm packages-microsoft-prod.deb

# Install .NET 8.0 SDK
sudo apt update
sudo apt install -y dotnet-sdk-8.0

# Install Git
sudo apt install -y git
```

#### CentOS/RHEL/Fedora
```bash
# Add Microsoft package repository
sudo rpm -Uvh https://packages.microsoft.com/config/centos/8/packages-microsoft-prod.rpm

# Install .NET 8.0 SDK
sudo dnf install -y dotnet-sdk-8.0

# Install Git
sudo dnf install -y git
```

#### Arch Linux
```bash
# Install from AUR
yay -S dotnet-sdk-8.0

# Or using official repositories
sudo pacman -S dotnet-sdk git
```

#### Verify Installation
```bash
# Check .NET version
dotnet --version

# Check Git version
git --version
```

### Step 2: Clone and Build

#### Clone the Repository
```bash
# Navigate to your development directory
cd ~/Development  # or your preferred location

# Clone the repository
git clone <repository-url>
cd MedicalAI.ThesisSuite
```

#### Build the Project
```bash
# Restore NuGet packages
dotnet restore

# Build in Release mode
dotnet build --configuration Release
```

### Step 3: Run the Application

#### Run from Source
```bash
# Run the UI application
dotnet run --project src/MedicalAI.UI

# Run the CLI application
dotnet run --project src/MedicalAI.CLI
```

#### Create and Run Executable
```bash
# Create self-contained executable
dotnet publish src/MedicalAI.UI -c Release -r linux-x64 -p:PublishSingleFile=true -p:SelfContained=true

# Navigate to output directory
cd src/MedicalAI.UI/bin/Release/net8.0/linux-x64/publish

# Make executable and run
chmod +x MedicalAI.UI
./MedicalAI.UI
```

### Step 4: Create Distribution Package
```bash
# Use the provided build script
chmod +x build/publish.sh
./build/publish.sh linux-x64

# This creates MedicalAI.ThesisSuite.zip in the root directory
```

## macOS Build Instructions

### Step 1: Install Prerequisites

#### Install .NET 8.0 SDK
**Option A: Using Homebrew (Recommended)**
```bash
# Install Homebrew if not already installed
/bin/bash -c "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)"

# Install .NET 8.0 SDK
brew install dotnet
```

**Option B: Manual Download**
1. Visit [.NET 8.0 Download Page](https://dotnet.microsoft.com/download/dotnet/8.0)
2. Download "SDK" for macOS (choose x64 or ARM64 based on your Mac)
3. Run the installer package
4. Restart your terminal

#### Install Git
```bash
# Git is usually pre-installed, but you can update it
brew install git
```

#### Verify Installation
```bash
# Check .NET version
dotnet --version

# Check Git version
git --version
```

### Step 2: Clone and Build

#### Clone the Repository
```bash
# Navigate to your development directory
cd ~/Development  # or your preferred location

# Clone the repository
git clone <repository-url>
cd MedicalAI.ThesisSuite
```

#### Build the Project
```bash
# Restore NuGet packages
dotnet restore

# Build in Release mode
dotnet build --configuration Release
```

### Step 3: Run the Application

#### Run from Source
```bash
# Run the UI application
dotnet run --project src/MedicalAI.UI

# Run the CLI application
dotnet run --project src/MedicalAI.CLI
```

#### Create and Run Executable
```bash
# For Intel Macs
dotnet publish src/MedicalAI.UI -c Release -r osx-x64 -p:PublishSingleFile=true -p:SelfContained=true

# For Apple Silicon Macs (M1/M2/M3)
dotnet publish src/MedicalAI.UI -c Release -r osx-arm64 -p:PublishSingleFile=true -p:SelfContained=true

# Navigate to output directory (adjust path based on your architecture)
cd src/MedicalAI.UI/bin/Release/net8.0/osx-x64/publish  # or osx-arm64

# Make executable and run
chmod +x MedicalAI.UI
./MedicalAI.UI
```

### Step 4: Create Distribution Package
```bash
# For Intel Macs
./build/publish.sh osx-x64

# For Apple Silicon Macs
./build/publish.sh osx-arm64

# This creates MedicalAI.ThesisSuite.zip in the root directory
```

## Development Environment Setup

### Visual Studio 2022 (Windows)
1. Install Visual Studio 2022 Community or higher
2. Ensure ".NET desktop development" workload is installed
3. Open `MedicalAI.ThesisSuite.sln`
4. Build → Build Solution (Ctrl+Shift+B)
5. Debug → Start Debugging (F5) or Start Without Debugging (Ctrl+F5)

### JetBrains Rider (Cross-platform)
1. Install JetBrains Rider
2. Open the solution directory
3. Rider will automatically detect the .NET solution
4. Use Run/Debug configurations for different projects

### Visual Studio Code (Cross-platform)
1. Install VS Code
2. Install the C# extension by Microsoft
3. Open the project folder
4. Use Ctrl+Shift+P → ".NET: Generate Assets for Build and Debug"
5. Use F5 to run with debugging

### Command Line Development
```bash
# Watch for changes and rebuild automatically
dotnet watch --project src/MedicalAI.UI

# Run tests in watch mode
dotnet watch test

# Hot reload for UI development
dotnet watch run --project src/MedicalAI.UI
```

## Troubleshooting

### Common Build Issues

#### Issue: "SDK not found" or "dotnet command not found"
**Solution:**
```bash
# Verify .NET installation
dotnet --info

# If not found, reinstall .NET SDK
# Follow the installation steps for your platform above
```

#### Issue: "Package restore failed"
**Solution:**
```bash
# Clear NuGet cache
dotnet nuget locals all --clear

# Restore packages with verbose output
dotnet restore --verbosity detailed
```

#### Issue: "Project reference could not be resolved"
**Solution:**
```bash
# Clean and rebuild
dotnet clean
dotnet restore
dotnet build
```

#### Issue: "NETSDK1045: The current .NET SDK does not support targeting .NET 8.0"
**Solution:**
- Ensure you have .NET 8.0 SDK installed (not just runtime)
- Update to the latest .NET 8.0 SDK version

### Platform-Specific Issues

#### Windows: "Execution policy" errors in PowerShell
```powershell
# Allow script execution (run as Administrator)
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
```

#### Linux: "Permission denied" when running executables
```bash
# Make the file executable
chmod +x MedicalAI.UI

# Or for the build script
chmod +x build/publish.sh
```

#### macOS: "App can't be opened because it is from an unidentified developer"
```bash
# Allow the app to run (one-time)
sudo spctl --master-disable

# Or right-click the app and select "Open"
```

### Performance Issues

#### Slow Build Times
```bash
# Use parallel builds
dotnet build --configuration Release --verbosity minimal

# Skip tests during build
dotnet build --no-restore --configuration Release
```

#### High Memory Usage During Build
```bash
# Limit parallel builds
dotnet build -m:1

# Or set environment variable
export DOTNET_CLI_TELEMETRY_OPTOUT=1
```

## Deployment and Distribution

### Creating Release Builds

#### Single-File Executables
```bash
# Windows x64
dotnet publish src/MedicalAI.UI -c Release -r win-x64 -p:PublishSingleFile=true -p:SelfContained=true

# Linux x64
dotnet publish src/MedicalAI.UI -c Release -r linux-x64 -p:PublishSingleFile=true -p:SelfContained=true

# macOS x64
dotnet publish src/MedicalAI.UI -c Release -r osx-x64 -p:PublishSingleFile=true -p:SelfContained=true

# macOS ARM64 (Apple Silicon)
dotnet publish src/MedicalAI.UI -c Release -r osx-arm64 -p:PublishSingleFile=true -p:SelfContained=true
```

#### Framework-Dependent Deployments (Smaller size)
```bash
# Requires .NET 8.0 Runtime on target machine
dotnet publish src/MedicalAI.UI -c Release -r win-x64 -p:PublishSingleFile=true -p:SelfContained=false
```

### Automated Build Scripts

The project includes automated build scripts in the `build/` directory:

#### Windows (PowerShell)
```powershell
# Build for current platform
.\build\publish.ps1

# Build for specific platform
.\build\publish.ps1 win-x64
.\build\publish.ps1 win-arm64
```

#### Linux/macOS (Bash)
```bash
# Build for current platform
./build/publish.sh

# Build for specific platform
./build/publish.sh linux-x64
./build/publish.sh osx-x64
./build/publish.sh osx-arm64
```

### Distribution Checklist

Before distributing the application:

1. **Test on Target Platform**: Verify the build works on a clean machine
2. **Include Documentation**: Ensure README and user guides are included
3. **Sample Data**: Include sample DICOM and NIfTI files for testing
4. **License Information**: Include all required license files
5. **Version Information**: Update version numbers in project files
6. **Security Scan**: Run security analysis on dependencies

### Continuous Integration

For automated builds, consider setting up CI/CD pipelines:

#### GitHub Actions Example
```yaml
name: Build and Test

on: [push, pull_request]

jobs:
  build:
    runs-on: ${{ matrix.os }}
    strategy:
      matrix:
        os: [windows-latest, ubuntu-latest, macos-latest]
    
    steps:
    - uses: actions/checkout@v3
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: 8.0.x
    - name: Restore dependencies
      run: dotnet restore
    - name: Build
      run: dotnet build --no-restore --configuration Release
    - name: Test
      run: dotnet test --no-build --configuration Release
```

## Next Steps

After successfully building and running the application:

1. **Explore the User Guide**: See [User Guide](user-guide.md) for detailed feature documentation
2. **Review API Documentation**: Check [API Documentation](api/) for development information
3. **Try Sample Workflows**: Use the included sample data to test functionality
4. **Customize Configuration**: Modify settings for your specific use case

For additional help, see the [Troubleshooting Guide](troubleshooting.md) or create an issue in the project repository.
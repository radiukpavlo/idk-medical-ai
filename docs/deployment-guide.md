# Deployment and Distribution Guide

This guide covers advanced deployment scenarios, distribution strategies, and production considerations for the MedicalAI Thesis Suite.

## Table of Contents
- [Deployment Types](#deployment-types)
- [Production Build Configuration](#production-build-configuration)
- [Distribution Strategies](#distribution-strategies)
- [Security Considerations](#security-considerations)
- [Performance Optimization](#performance-optimization)
- [Monitoring and Logging](#monitoring-and-logging)
- [Update and Maintenance](#update-and-maintenance)

## Deployment Types

### Self-Contained Deployment (Recommended)

Self-contained deployments include the .NET runtime, making them ideal for distribution to systems without .NET installed.

#### Advantages
- No .NET runtime dependency on target machine
- Consistent runtime version across deployments
- Easier distribution and installation
- Better isolation from system changes

#### Disadvantages
- Larger file size (100-150 MB per platform)
- Separate builds required for each platform

#### Build Commands
```bash
# Windows x64
dotnet publish src/MedicalAI.UI -c Release -r win-x64 \
  -p:PublishSingleFile=true \
  -p:SelfContained=true \
  -p:PublishTrimmed=true \
  -p:TrimMode=partial

# Linux x64
dotnet publish src/MedicalAI.UI -c Release -r linux-x64 \
  -p:PublishSingleFile=true \
  -p:SelfContained=true \
  -p:PublishTrimmed=true \
  -p:TrimMode=partial

# macOS x64
dotnet publish src/MedicalAI.UI -c Release -r osx-x64 \
  -p:PublishSingleFile=true \
  -p:SelfContained=true \
  -p:PublishTrimmed=true \
  -p:TrimMode=partial

# macOS ARM64 (Apple Silicon)
dotnet publish src/MedicalAI.UI -c Release -r osx-arm64 \
  -p:PublishSingleFile=true \
  -p:SelfContained=true \
  -p:PublishTrimmed=true \
  -p:TrimMode=partial
```

### Framework-Dependent Deployment

Framework-dependent deployments require .NET 8.0 runtime on the target machine but result in smaller file sizes.

#### Advantages
- Smaller file size (10-20 MB)
- Automatic runtime updates from system
- Shared runtime across applications

#### Disadvantages
- Requires .NET 8.0 runtime installation
- Potential compatibility issues with runtime updates
- More complex deployment requirements

#### Build Commands
```bash
# Cross-platform framework-dependent
dotnet publish src/MedicalAI.UI -c Release \
  -p:PublishSingleFile=true \
  -p:SelfContained=false

# Platform-specific framework-dependent
dotnet publish src/MedicalAI.UI -c Release -r win-x64 \
  -p:PublishSingleFile=true \
  -p:SelfContained=false
```

## Production Build Configuration

### Optimized Build Settings

Create a production-specific publish profile for optimal performance:

#### PublishProfiles/Production.pubxml
```xml
<?xml version="1.0" encoding="utf-8"?>
<Project ToolsVersion="4.0" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <PropertyGroup>
    <Configuration>Release</Configuration>
    <Platform>Any CPU</Platform>
    <PublishUrl>bin\Release\net8.0\publish\</PublishUrl>
    <PublishProtocol>FileSystem</PublishProtocol>
    <TargetFramework>net8.0</TargetFramework>
    <SelfContained>true</SelfContained>
    <PublishSingleFile>true</PublishSingleFile>
    <PublishTrimmed>true</PublishTrimmed>
    <TrimMode>partial</TrimMode>
    <PublishReadyToRun>true</PublishReadyToRun>
    <IncludeNativeLibrariesForSelfExtract>true</IncludeNativeLibrariesForSelfExtract>
    <DebugType>none</DebugType>
    <DebugSymbols>false</DebugSymbols>
  </PropertyGroup>
</Project>
```

#### Using the Profile
```bash
dotnet publish src/MedicalAI.UI -p:PublishProfile=Production -r win-x64
```

### Build Optimization Options

#### Assembly Trimming
```xml
<PropertyGroup>
  <PublishTrimmed>true</PublishTrimmed>
  <TrimMode>partial</TrimMode>
  <!-- Preserve specific assemblies if needed -->
  <TrimmerRootAssembly Include="MedicalAI.Core" />
</PropertyGroup>
```

#### ReadyToRun Compilation
```xml
<PropertyGroup>
  <PublishReadyToRun>true</PublishReadyToRun>
  <PublishReadyToRunShowWarnings>true</PublishReadyToRunShowWarnings>
</PropertyGroup>
```

#### Native AOT (Experimental)
```xml
<PropertyGroup>
  <PublishAot>true</PublishAot>
  <InvariantGlobalization>true</InvariantGlobalization>
</PropertyGroup>
```

## Distribution Strategies

### Desktop Application Distribution

#### Windows Distribution

**Option 1: ZIP Archive (Current Method)**
```powershell
# Create distribution package
.\build\publish.ps1 win-x64

# Results in MedicalAI.ThesisSuite.zip
```

**Option 2: Windows Installer (MSI)**
```xml
<!-- Add to UI project file -->
<PropertyGroup>
  <GeneratePackageOnBuild>true</GeneratePackageOnBuild>
  <PackageId>MedicalAI.ThesisSuite</PackageId>
  <PackageVersion>1.0.0</PackageVersion>
  <Authors>Medical AI Research Team</Authors>
  <Description>AI-powered medical diagnostic suite</Description>
</PropertyGroup>
```

**Option 3: Microsoft Store Package (MSIX)**
```bash
# Install MSIX packaging tools
# Create MSIX package for Microsoft Store distribution
```

#### Linux Distribution

**Option 1: Portable Archive**
```bash
./build/publish.sh linux-x64
```

**Option 2: AppImage**
```bash
# Create AppImage for universal Linux distribution
# Requires AppImage tools
```

**Option 3: Package Managers**
```bash
# Create .deb package for Debian/Ubuntu
# Create .rpm package for Red Hat/Fedora
# Create AUR package for Arch Linux
```

#### macOS Distribution

**Option 1: Portable Archive**
```bash
./build/publish.sh osx-x64  # Intel Macs
./build/publish.sh osx-arm64  # Apple Silicon Macs
```

**Option 2: macOS Application Bundle (.app)**
```bash
# Create proper .app bundle structure
mkdir -p MedicalAI.app/Contents/MacOS
mkdir -p MedicalAI.app/Contents/Resources

# Copy executable and create Info.plist
cp MedicalAI.UI MedicalAI.app/Contents/MacOS/
```

**Option 3: macOS Installer Package (.pkg)**
```bash
# Use pkgbuild and productbuild for installer creation
```

### Enterprise Distribution

#### Network Deployment
```bash
# Deploy to network share
\\server\software\MedicalAI\

# Create deployment script
@echo off
xcopy "\\server\software\MedicalAI\*" "C:\Program Files\MedicalAI\" /E /Y
```

#### Group Policy Deployment (Windows)
1. Create MSI package
2. Deploy via Group Policy Software Installation
3. Configure automatic updates

#### Configuration Management
```yaml
# Ansible playbook example
- name: Deploy MedicalAI Suite
  hosts: workstations
  tasks:
    - name: Download application
      get_url:
        url: "https://releases.example.com/MedicalAI.ThesisSuite.zip"
        dest: "/tmp/MedicalAI.zip"
    
    - name: Extract application
      unarchive:
        src: "/tmp/MedicalAI.zip"
        dest: "/opt/MedicalAI/"
        remote_src: yes
```

## Security Considerations

### Code Signing

#### Windows Code Signing
```powershell
# Sign executable with certificate
signtool sign /f certificate.pfx /p password /t http://timestamp.digicert.com MedicalAI.UI.exe

# Verify signature
signtool verify /pa MedicalAI.UI.exe
```

#### macOS Code Signing
```bash
# Sign application
codesign --sign "Developer ID Application: Your Name" MedicalAI.UI

# Verify signature
codesign --verify --verbose MedicalAI.UI

# Notarize for Gatekeeper
xcrun notarytool submit MedicalAI.zip --keychain-profile "notary-profile"
```

### Security Hardening

#### Application Security
```xml
<!-- Enable security features in project file -->
<PropertyGroup>
  <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  <WarningsAsErrors />
  <EnableNETAnalyzers>true</EnableNETAnalyzers>
  <AnalysisLevel>latest</AnalysisLevel>
</PropertyGroup>
```

#### Runtime Security
```csharp
// Configure security policies
public static void ConfigureSecurity(IServiceCollection services)
{
    services.Configure<SecurityOptions>(options =>
    {
        options.RequireHttps = true;
        options.ValidateAntiForgeryToken = true;
        options.EnableContentSecurityPolicy = true;
    });
}
```

### Medical Data Security

#### DICOM Anonymization
```csharp
// Ensure proper anonymization settings
var anonymizer = new DicomAnonymizer();
anonymizer.Profile = DicomAnonymizer.SecurityProfile.BasicProfile;
anonymizer.AnonymizeInPlace = false; // Always create copies
```

#### Data Encryption
```csharp
// Encrypt sensitive data at rest
public class EncryptedDataService
{
    private readonly IDataProtectionProvider _dataProtection;
    
    public string ProtectData(string data)
    {
        var protector = _dataProtection.CreateProtector("MedicalData");
        return protector.Protect(data);
    }
}
```

## Performance Optimization

### Build-Time Optimizations

#### Compilation Settings
```xml
<PropertyGroup Condition="'$(Configuration)' == 'Release'">
  <Optimize>true</Optimize>
  <DebugType>none</DebugType>
  <DebugSymbols>false</DebugSymbols>
  <DefineConstants>RELEASE</DefineConstants>
</PropertyGroup>
```

#### Assembly Optimization
```xml
<PropertyGroup>
  <!-- Enable all optimizations -->
  <PublishTrimmed>true</PublishTrimmed>
  <PublishReadyToRun>true</PublishReadyToRun>
  <TieredCompilation>true</TieredCompilation>
  <TieredPGO>true</TieredPGO>
</PropertyGroup>
```

### Runtime Optimizations

#### Memory Management
```csharp
// Configure garbage collection
public static void ConfigureGC()
{
    GCSettings.LatencyMode = GCLatencyMode.SustainedLowLatency;
    GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
}
```

#### Image Processing Optimization
```csharp
// Use memory-efficient image processing
public class OptimizedImageProcessor
{
    public async Task<ProcessedImage> ProcessLargeImageAsync(Stream imageStream)
    {
        using var image = await Image.LoadAsync(imageStream);
        
        // Process in chunks to manage memory
        var processor = new ChunkedImageProcessor();
        return await processor.ProcessAsync(image);
    }
}
```

## Monitoring and Logging

### Application Monitoring

#### Health Checks
```csharp
public class ApplicationHealthCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, 
        CancellationToken cancellationToken = default)
    {
        // Check critical application components
        var isHealthy = CheckDatabaseConnection() && 
                       CheckModelAvailability() && 
                       CheckMemoryUsage();
        
        return Task.FromResult(isHealthy 
            ? HealthCheckResult.Healthy() 
            : HealthCheckResult.Unhealthy());
    }
}
```

#### Performance Counters
```csharp
public class PerformanceMonitor
{
    private readonly ILogger<PerformanceMonitor> _logger;
    private readonly PerformanceCounter _cpuCounter;
    private readonly PerformanceCounter _memoryCounter;
    
    public void LogPerformanceMetrics()
    {
        var cpuUsage = _cpuCounter.NextValue();
        var memoryUsage = _memoryCounter.NextValue();
        
        _logger.LogInformation("CPU: {CpuUsage}%, Memory: {MemoryUsage}MB", 
            cpuUsage, memoryUsage);
    }
}
```

### Logging Configuration

#### Production Logging
```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning"
      }
    },
    "WriteTo": [
      {
        "Name": "File",
        "Args": {
          "path": "logs/medicalai-.log",
          "rollingInterval": "Day",
          "retainedFileCountLimit": 30,
          "formatter": "Serilog.Formatting.Compact.CompactJsonFormatter, Serilog.Formatting.Compact"
        }
      }
    ]
  }
}
```

#### Error Tracking
```csharp
public class GlobalExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    
    public void HandleException(Exception exception)
    {
        _logger.LogError(exception, "Unhandled exception occurred");
        
        // Send to error tracking service if configured
        // ErrorTrackingService.ReportException(exception);
    }
}
```

## Update and Maintenance

### Automatic Updates

#### Update Check Service
```csharp
public class UpdateService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<UpdateService> _logger;
    
    public async Task<UpdateInfo> CheckForUpdatesAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("https://api.example.com/updates/check");
            var updateInfo = await response.Content.ReadFromJsonAsync<UpdateInfo>();
            
            return updateInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check for updates");
            return null;
        }
    }
}
```

#### Update Installation
```csharp
public class UpdateInstaller
{
    public async Task<bool> InstallUpdateAsync(UpdateInfo update)
    {
        // Download update package
        var updatePackage = await DownloadUpdateAsync(update.DownloadUrl);
        
        // Verify signature
        if (!VerifyUpdateSignature(updatePackage))
        {
            throw new SecurityException("Update package signature verification failed");
        }
        
        // Install update
        return await ApplyUpdateAsync(updatePackage);
    }
}
```

### Maintenance Tasks

#### Database Maintenance
```csharp
public class MaintenanceService
{
    public async Task PerformMaintenanceAsync()
    {
        // Clean up temporary files
        await CleanupTempFilesAsync();
        
        // Optimize database
        await OptimizeDatabaseAsync();
        
        // Archive old logs
        await ArchiveOldLogsAsync();
        
        // Check for corrupted data
        await ValidateDataIntegrityAsync();
    }
}
```

#### Configuration Backup
```csharp
public class ConfigurationBackup
{
    public async Task BackupConfigurationAsync()
    {
        var configPath = Path.Combine(Environment.GetFolderPath(
            Environment.SpecialFolder.ApplicationData), "MedicalAI");
        
        var backupPath = Path.Combine(configPath, "Backups", 
            $"config-{DateTime.Now:yyyyMMdd-HHmmss}.zip");
        
        await CreateBackupArchiveAsync(configPath, backupPath);
    }
}
```

### Rollback Procedures

#### Version Rollback
```bash
# Keep previous version for rollback
cp MedicalAI.UI MedicalAI.UI.backup

# Install new version
cp MedicalAI.UI.new MedicalAI.UI

# Rollback if needed
cp MedicalAI.UI.backup MedicalAI.UI
```

#### Configuration Rollback
```csharp
public class ConfigurationRollback
{
    public async Task RollbackConfigurationAsync(string backupPath)
    {
        var configPath = GetConfigurationPath();
        
        // Backup current configuration
        await BackupCurrentConfigurationAsync();
        
        // Restore from backup
        await ExtractBackupAsync(backupPath, configPath);
        
        // Validate restored configuration
        if (!await ValidateConfigurationAsync())
        {
            throw new InvalidOperationException("Configuration rollback failed validation");
        }
    }
}
```

## Deployment Checklist

### Pre-Deployment
- [ ] All tests pass
- [ ] Security scan completed
- [ ] Performance benchmarks meet requirements
- [ ] Documentation updated
- [ ] Version numbers incremented
- [ ] Code signed (if required)
- [ ] Backup procedures tested

### Deployment
- [ ] Build with production configuration
- [ ] Verify all dependencies included
- [ ] Test on clean target environment
- [ ] Validate security settings
- [ ] Confirm logging configuration
- [ ] Test update mechanisms

### Post-Deployment
- [ ] Monitor application startup
- [ ] Verify core functionality
- [ ] Check performance metrics
- [ ] Review error logs
- [ ] Validate security measures
- [ ] Test rollback procedures
- [ ] Update documentation

## Best Practices

### Security
1. Always sign production builds
2. Use secure update mechanisms
3. Implement proper error handling
4. Follow medical data privacy regulations
5. Regular security audits

### Performance
1. Profile before optimizing
2. Monitor memory usage
3. Use appropriate caching strategies
4. Optimize for target hardware
5. Regular performance testing

### Maintenance
1. Automated backup procedures
2. Regular update schedules
3. Monitoring and alerting
4. Documentation maintenance
5. User training and support

This deployment guide ensures reliable, secure, and maintainable distribution of the MedicalAI Thesis Suite across various environments and use cases.
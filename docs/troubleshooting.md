# Troubleshooting Guide

This guide helps resolve common issues when building, running, or using the MedicalAI Thesis Suite.

## Table of Contents
- [Build and Compilation Issues](#build-and-compilation-issues)
- [Runtime and Application Issues](#runtime-and-application-issues)
- [Platform-Specific Issues](#platform-specific-issues)
- [Performance Issues](#performance-issues)
- [Medical Data Processing Issues](#medical-data-processing-issues)
- [UI and Display Issues](#ui-and-display-issues)
- [Getting Help](#getting-help)

## Build and Compilation Issues

### .NET SDK Issues

#### Problem: "dotnet command not found" or "SDK not found"
**Symptoms:**
- Command prompt doesn't recognize `dotnet` command
- Error: "A compatible installed .NET SDK for global.json version"

**Solutions:**
1. **Verify Installation:**
   ```bash
   # Check if .NET is installed
   dotnet --version
   dotnet --info
   ```

2. **Reinstall .NET SDK:**
   - Download from [Microsoft .NET Downloads](https://dotnet.microsoft.com/download/dotnet/8.0)
   - Ensure you download the SDK, not just the Runtime

3. **Fix PATH Environment Variable:**
   ```bash
   # Windows (PowerShell as Administrator)
   $env:PATH += ";C:\Program Files\dotnet"
   
   # Linux/macOS
   export PATH=$PATH:/usr/share/dotnet
   ```

4. **Restart Terminal/IDE** after installation

#### Problem: "The current .NET SDK does not support targeting .NET 8.0"
**Solutions:**
1. Update to .NET 8.0 SDK or later
2. Check global.json file for version constraints
3. Verify project target framework in .csproj files

### Package Restore Issues

#### Problem: "Package restore failed" or "Unable to load the service index"
**Symptoms:**
- NuGet packages fail to download
- Network-related errors during restore

**Solutions:**
1. **Clear NuGet Cache:**
   ```bash
   dotnet nuget locals all --clear
   dotnet restore --force
   ```

2. **Check Network Connection:**
   - Verify internet connectivity
   - Check corporate firewall/proxy settings
   - Configure NuGet proxy if needed

3. **Update NuGet Sources:**
   ```bash
   dotnet nuget list source
   dotnet nuget add source https://api.nuget.org/v3/index.json -n nuget.org
   ```

### Compilation Errors

#### Problem: "CS0246: The type or namespace name could not be found"
**Solutions:**
1. **Restore Packages:**
   ```bash
   dotnet restore
   dotnet build --no-restore
   ```

2. **Check Project References:**
   - Verify all project references are correct
   - Ensure referenced projects build successfully

3. **Clean and Rebuild:**
   ```bash
   dotnet clean
   dotnet restore
   dotnet build
   ```

#### Problem: "CS8600: Converting null literal or possible null value to non-nullable type"
**Solutions:**
1. **Enable Nullable Reference Types** (if not already enabled)
2. **Add Null Checks:**
   ```csharp
   if (value != null)
   {
       // Use value
   }
   ```
3. **Use Null-Forgiving Operator** (when you're certain value is not null):
   ```csharp
   var result = value!.ToString();
   ```

## Runtime and Application Issues

### Application Startup Issues

#### Problem: Application fails to start or crashes immediately
**Symptoms:**
- Application window doesn't appear
- Immediate crash with error dialog
- Console shows unhandled exceptions

**Solutions:**
1. **Check Dependencies:**
   ```bash
   # Verify all required files are present
   dotnet MedicalAI.UI.dll --help
   ```

2. **Run with Verbose Logging:**
   ```bash
   # Set environment variable for detailed logging
   export DOTNET_ENVIRONMENT=Development
   dotnet run --project src/MedicalAI.UI
   ```

3. **Check System Requirements:**
   - Ensure minimum RAM (4GB) is available
   - Verify graphics drivers are up to date
   - Check disk space availability

#### Problem: "Could not load file or assembly" errors
**Solutions:**
1. **Verify Runtime Installation:**
   ```bash
   dotnet --list-runtimes
   ```

2. **Use Self-Contained Deployment:**
   ```bash
   dotnet publish -c Release -r win-x64 -p:SelfContained=true
   ```

3. **Check Assembly Binding:**
   - Review dependency versions in .csproj files
   - Ensure compatible package versions

### Configuration Issues

#### Problem: Application settings not loading correctly
**Solutions:**
1. **Check Configuration Files:**
   - Verify appsettings.json exists and is valid
   - Check file permissions

2. **Reset to Defaults:**
   - Delete user configuration files
   - Restart application to regenerate defaults

3. **Validate JSON Syntax:**
   - Use JSON validator to check configuration files
   - Look for missing commas or brackets

## Platform-Specific Issues

### Windows Issues

#### Problem: "Windows protected your PC" SmartScreen warning
**Solutions:**
1. **Click "More info" → "Run anyway"** (for trusted builds)
2. **Sign the Executable** (for distribution)
3. **Add Exception to Windows Defender**

#### Problem: PowerShell execution policy errors
**Solutions:**
```powershell
# Allow script execution (run as Administrator)
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser

# Or bypass for single script
PowerShell -ExecutionPolicy Bypass -File .\build\publish.ps1
```

### Linux Issues

#### Problem: "Permission denied" when running executable
**Solutions:**
```bash
# Make file executable
chmod +x MedicalAI.UI

# Or for all files in directory
chmod +x *
```

#### Problem: Missing system libraries
**Solutions:**
```bash
# Ubuntu/Debian - install common dependencies
sudo apt install -y libicu-dev libssl-dev

# CentOS/RHEL/Fedora
sudo dnf install -y icu libssl-dev

# Check missing dependencies
ldd MedicalAI.UI
```

#### Problem: Display/GUI issues on headless systems
**Solutions:**
```bash
# Install X11 forwarding for SSH
ssh -X username@hostname

# Or use virtual display
export DISPLAY=:0.0
```

### macOS Issues

#### Problem: "App can't be opened because it is from an unidentified developer"
**Solutions:**
1. **Right-click → Open** (bypass Gatekeeper once)
2. **System Preferences → Security & Privacy → Allow**
3. **Command Line Override:**
   ```bash
   sudo spctl --master-disable  # Disable Gatekeeper (not recommended)
   ```

#### Problem: Rosetta 2 compatibility on Apple Silicon
**Solutions:**
```bash
# Install Rosetta 2 for x64 compatibility
softwareupdate --install-rosetta

# Or build native ARM64 version
dotnet publish -r osx-arm64 -c Release
```

## Performance Issues

### Slow Application Performance

#### Problem: Application is slow or unresponsive
**Symptoms:**
- Long loading times
- UI freezes during operations
- High CPU/memory usage

**Solutions:**
1. **Check System Resources:**
   ```bash
   # Monitor resource usage
   # Windows: Task Manager
   # Linux: htop or top
   # macOS: Activity Monitor
   ```

2. **Optimize Memory Settings:**
   ```bash
   # Increase available memory
   export DOTNET_GCHeapHardLimit=4000000000  # 4GB limit
   ```

3. **Profile Application:**
   ```bash
   # Run with profiling
   dotnet run --project src/MedicalAI.UI --configuration Release
   ```

### Large File Processing Issues

#### Problem: Out of memory errors with large medical images
**Solutions:**
1. **Increase Available Memory:**
   - Close other applications
   - Use 64-bit build on systems with >4GB RAM

2. **Process Files in Chunks:**
   - Use streaming for large files
   - Process images in smaller segments

3. **Optimize Image Processing:**
   - Reduce image resolution for preview
   - Use efficient image formats

## Medical Data Processing Issues

### DICOM File Issues

#### Problem: "Unable to load DICOM file" or "Invalid DICOM format"
**Solutions:**
1. **Verify File Format:**
   - Ensure file has .dcm extension
   - Check file is not corrupted
   - Verify DICOM compliance

2. **Check File Permissions:**
   ```bash
   # Ensure read permissions
   chmod 644 filename.dcm
   ```

3. **Test with Sample Data:**
   - Use provided sample.dcm file
   - Verify application works with known good files

#### Problem: DICOM anonymization not working
**Solutions:**
1. **Check Anonymization Settings:**
   - Verify anonymization is enabled in settings
   - Review anonymization rules

2. **Backup Original Files:**
   - Always keep original files safe
   - Test anonymization on copies

### NIfTI File Issues

#### Problem: "Cannot read NIfTI file" or "Unsupported NIfTI format"
**Solutions:**
1. **Verify File Format:**
   - Check file extension (.nii or .nii.gz)
   - Ensure file is valid NIfTI format

2. **Check Compression:**
   - Try uncompressed .nii files
   - Verify gzip support for .nii.gz files

### AI Model Issues

#### Problem: "Model inference failed" or "ONNX Runtime error"
**Solutions:**
1. **Check Model Files:**
   - Verify .onnx model files exist in models/ directory
   - Check file permissions and integrity

2. **Fallback to Mock Inference:**
   - Application should automatically use mock inference
   - Check logs for fallback behavior

3. **Update ONNX Runtime:**
   - Ensure latest compatible ONNX Runtime version
   - Check GPU driver compatibility for GPU inference

## UI and Display Issues

### Avalonia UI Issues

#### Problem: UI elements not displaying correctly
**Solutions:**
1. **Update Graphics Drivers:**
   - Install latest graphics drivers
   - Restart system after driver update

2. **Check Display Scaling:**
   - Adjust system display scaling settings
   - Test with 100% scaling

3. **Try Different Rendering Backend:**
   ```bash
   # Force software rendering
   export AVALONIA_RENDERING_MODE=Software
   ```

#### Problem: Fonts not displaying correctly
**Solutions:**
1. **Install Required Fonts:**
   - Ensure system has required fonts installed
   - Check font licensing for distribution

2. **Font Fallback:**
   - Application should use system default fonts
   - Check font configuration in application settings

### Visualization Issues

#### Problem: Medical images not displaying or appearing corrupted
**Solutions:**
1. **Check Image Format Support:**
   - Verify supported image formats
   - Test with different image files

2. **Graphics Memory:**
   - Ensure sufficient graphics memory
   - Reduce image resolution if needed

3. **Color Space Issues:**
   - Check image color space settings
   - Verify display calibration

## Getting Help

### Diagnostic Information

When reporting issues, please include:

1. **System Information:**
   ```bash
   # .NET version
   dotnet --info
   
   # Operating system version
   # Windows: winver
   # Linux: uname -a
   # macOS: sw_vers
   ```

2. **Application Logs:**
   - Check application log files
   - Include relevant error messages
   - Note steps to reproduce the issue

3. **Build Information:**
   - Build configuration (Debug/Release)
   - Target platform (win-x64, linux-x64, etc.)
   - Self-contained vs framework-dependent

### Log Files Location

Application logs are typically stored in:
- **Windows:** `%APPDATA%\MedicalAI\Logs\`
- **Linux:** `~/.config/MedicalAI/Logs/`
- **macOS:** `~/Library/Application Support/MedicalAI/Logs/`

### Reporting Issues

1. **Check Existing Issues:** Review [project issues](../../issues) for similar problems
2. **Create Detailed Report:** Include system info, steps to reproduce, and error messages
3. **Provide Sample Data:** If possible, include sample files that cause the issue
4. **Follow Up:** Respond to questions and provide additional information as requested

### Community Resources

- **Documentation:** Check all documentation in the `docs/` directory
- **Sample Code:** Review example implementations in the source code
- **Test Cases:** Look at test projects for usage examples

### Emergency Workarounds

If you need to quickly resolve critical issues:

1. **Use Previous Version:** Revert to last known working version
2. **Minimal Configuration:** Start with default settings
3. **Safe Mode:** Run with minimal features enabled
4. **Alternative Tools:** Use other DICOM/NIfTI viewers for urgent work

Remember: This software is for research purposes only. For critical medical workflows, always have backup tools and procedures available.
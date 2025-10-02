# Build System Documentation

This directory contains the build automation and development tools for the MedicalAI Thesis Suite project.

## Overview

The build system provides comprehensive automation for:
- Cross-platform building and packaging
- Automated testing with coverage
- Code quality and formatting checks
- Development environment setup
- Deployment and distribution

## Quick Start

### Windows
```powershell
# Setup development environment
./build/setup-dev.ps1 -InstallTools -SetupGit

# Build for current platform
./build/publish.ps1

# Build for all platforms
./build/build-all.ps1

# Run tests with coverage
./build/test.ps1 -Coverage

# Check code quality
./build/code-quality.ps1 -Format -Analyze
```

### Linux/macOS
```bash
# Setup development environment
./build/setup-dev.sh --install-tools --setup-git

# Build for current platform
./build/publish.sh

# Build for all platforms
./build/build-all.sh

# Run tests with coverage
./build/test.sh --coverage

# Check code quality
./build/code-quality.sh --format --analyze
```

## Build Scripts

### Core Build Scripts

#### `publish.ps1` / `publish.sh`
Main build and packaging script for single platform.

**Parameters:**
- `Configuration`: Build configuration (Debug/Release)
- `Runtime`: Target runtime (win-x64, linux-x64, osx-x64, etc.)
- `RunTests`: Run tests before publishing
- `OutputPath`: Output directory for packages

**Example:**
```bash
./build/publish.sh --configuration Release --runtime linux-x64 --run-tests
```

#### `build-all.ps1` / `build-all.sh`
Build for all supported platforms.

**Features:**
- Builds for all supported runtimes
- Generates build report
- Parallel execution where possible
- Comprehensive error handling

#### `test.ps1` / `test.sh`
Comprehensive test runner with coverage support.

**Features:**
- Unit and integration test execution
- Code coverage collection
- HTML coverage reports
- Test result aggregation

### Development Tools

#### `setup-dev.ps1` / `setup-dev.sh`
Development environment setup and configuration.

**Features:**
- Install development tools
- Configure VS Code settings
- Setup Git hooks
- Create development documentation

#### `code-quality.ps1` / `code-quality.sh`
Code quality and formatting tools.

**Features:**
- Code formatting with dotnet-format
- Static code analysis
- Security vulnerability scanning
- Outdated package detection

## Configuration Files

### `build.config.json`
Main build configuration file containing:
- Project metadata
- Build settings
- Runtime configurations
- Packaging options
- Testing configuration

### `debug-config.json`
Debugging and profiling configuration:
- Debugging settings
- Profiling options
- Logging configuration
- Diagnostics settings

### `coverlet.runsettings`
Code coverage configuration for Coverlet:
- Coverage format settings
- Exclusion rules
- Include/exclude patterns

## Supported Platforms

### Primary Platforms
- Windows x64 (`win-x64`)
- Linux x64 (`linux-x64`)
- macOS x64 (`osx-x64`)

### Secondary Platforms
- Windows x86 (`win-x86`)
- Windows ARM64 (`win-arm64`)
- Linux ARM64 (`linux-arm64`)
- macOS ARM64 (`osx-arm64`)

## Output Structure

```
dist/
├── MedicalAI.ThesisSuite-win-x64-20241002.zip
├── MedicalAI.ThesisSuite-linux-x64-20241002.tar.gz
├── MedicalAI.ThesisSuite-osx-x64-20241002.tar.gz
├── build-report-20241002-143022.json
├── code-quality-report-20241002-143022.json
└── test-results/
    ├── coverage.cobertura.xml
    └── coverage-html/
        └── index.html
```

## Development Workflow

### 1. Initial Setup
```bash
# Clone repository
git clone <repository-url>
cd MedicalAI.ThesisSuite

# Setup development environment
./build/setup-dev.sh --install-tools --setup-git

# Verify setup
dotnet build
dotnet test
```

### 2. Daily Development
```bash
# Format code
./build/code-quality.sh --format --fix

# Run tests
./build/test.sh

# Build and test
./build/publish.sh --run-tests
```

### 3. Release Preparation
```bash
# Check code quality
./build/code-quality.sh --analyze --check-outdated

# Build all platforms
./build/build-all.sh --run-tests

# Verify packages
ls -la dist/
```

## Continuous Integration

### GitHub Actions Template
The build system includes templates for CI/CD pipelines:

```yaml
name: Build and Test
on: [push, pull_request]
jobs:
  build:
    runs-on: ubuntu-latest
    steps:
    - uses: actions/checkout@v3
    - uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '8.0.x'
    - run: ./build/build-all.sh --run-tests
```

### Local CI Simulation
```bash
# Simulate CI environment
docker run --rm -v $(pwd):/workspace -w /workspace mcr.microsoft.com/dotnet/sdk:8.0 \
  bash -c "./build/build-all.sh --run-tests"
```

## Troubleshooting

### Common Issues

1. **Build Failures**
   - Check .NET SDK version: `dotnet --version`
   - Restore packages: `dotnet restore`
   - Clean build: `dotnet clean`

2. **Test Failures**
   - Check test output in `dist/test-results/`
   - Run specific tests: `dotnet test --filter "TestName"`
   - Enable verbose logging: `./build/test.sh --verbose`

3. **Platform-Specific Issues**
   - Windows: Ensure PowerShell execution policy allows scripts
   - Linux/macOS: Ensure scripts are executable: `chmod +x build/*.sh`
   - Cross-platform: Check runtime availability: `dotnet --list-runtimes`

### Debug Mode
Enable verbose output for detailed information:
```bash
./build/publish.sh --verbose
./build/test.sh --verbose
```

### Log Files
Build logs are saved to:
- `dist/build-report-*.json` - Build results
- `dist/code-quality-report-*.json` - Quality analysis
- `dist/test-results/` - Test results and coverage

## Advanced Usage

### Custom Build Configuration
Modify `build.config.json` to customize:
- Target frameworks
- Runtime identifiers
- Package inclusion rules
- Test settings

### Plugin Development
For AI plugin development:
1. Follow plugin architecture in `docs/plugin-development-guide.md`
2. Use plugin templates in `src/MedicalAI.Plugins/`
3. Test plugins with mock data in `datasets/samples/`

### Performance Profiling
Enable profiling in `debug-config.json`:
```json
{
  "profiling": {
    "enableProfiling": true,
    "memoryProfiling": {
      "enableMemoryProfiling": true
    }
  }
}
```

## Contributing

When contributing to the build system:
1. Test changes on all supported platforms
2. Update documentation for new features
3. Maintain backward compatibility
4. Follow existing script patterns and conventions

## Support

For build system issues:
1. Check this documentation
2. Review troubleshooting section
3. Check GitHub Issues
4. Contact the development team
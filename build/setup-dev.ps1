param(
    [switch]$InstallTools = $false,
    [switch]$ConfigureVS = $false,
    [switch]$SetupGit = $false,
    [switch]$Verbose = $false
)

$ErrorActionPreference = "Stop"

Write-Host "=== MedicalAI Thesis Suite Development Environment Setup ===" -ForegroundColor Green

# Check prerequisites
Write-Host "Checking prerequisites..." -ForegroundColor Yellow

# Check .NET SDK
try {
    $dotnetVersion = dotnet --version
    Write-Host "✓ .NET SDK: $dotnetVersion" -ForegroundColor Green
} catch {
    Write-Host "✗ .NET SDK not found. Please install .NET 8.0 SDK" -ForegroundColor Red
    exit 1
}

# Check Git
try {
    $gitVersion = git --version
    Write-Host "✓ Git: $gitVersion" -ForegroundColor Green
} catch {
    Write-Host "✗ Git not found. Please install Git" -ForegroundColor Red
    exit 1
}

# Install development tools if requested
if ($InstallTools) {
    Write-Host "Installing development tools..." -ForegroundColor Yellow
    
    # Install global .NET tools
    $tools = @(
        @{ Name = "dotnet-ef"; Package = "dotnet-ef"; Description = "Entity Framework Core tools" },
        @{ Name = "dotnet-reportgenerator-globaltool"; Package = "dotnet-reportgenerator-globaltool"; Description = "Code coverage report generator" },
        @{ Name = "dotnet-outdated-tool"; Package = "dotnet-outdated-tool"; Description = "Check for outdated NuGet packages" },
        @{ Name = "dotnet-format"; Package = "dotnet-format"; Description = "Code formatter" }
    )
    
    foreach ($tool in $tools) {
        Write-Host "Installing $($tool.Description)..." -ForegroundColor Cyan
        try {
            dotnet tool install -g $tool.Package 2>$null
            Write-Host "✓ Installed $($tool.Name)" -ForegroundColor Green
        } catch {
            Write-Host "- $($tool.Name) already installed or failed to install" -ForegroundColor Yellow
        }
    }
}

# Create development configuration files
Write-Host "Creating development configuration files..." -ForegroundColor Yellow

# Create .editorconfig if it doesn't exist
$editorConfigPath = ".editorconfig"
if (!(Test-Path $editorConfigPath)) {
    $editorConfig = @"
root = true

[*]
charset = utf-8
end_of_line = crlf
insert_final_newline = true
trim_trailing_whitespace = true

[*.{cs,csx,vb,vbx}]
indent_style = space
indent_size = 4

[*.{json,js,ts,html,css,scss,yml,yaml}]
indent_style = space
indent_size = 2

[*.md]
trim_trailing_whitespace = false

# C# formatting rules
[*.cs]
# Organize usings
dotnet_sort_system_directives_first = true
dotnet_separate_import_directive_groups = false

# Code style rules
dotnet_style_qualification_for_field = false:suggestion
dotnet_style_qualification_for_property = false:suggestion
dotnet_style_qualification_for_method = false:suggestion
dotnet_style_qualification_for_event = false:suggestion
dotnet_style_predefined_type_for_locals_parameters_members = true:suggestion
dotnet_style_predefined_type_for_member_access = true:suggestion
dotnet_style_require_accessibility_modifiers = for_non_interface_members:suggestion
dotnet_style_readonly_field = true:suggestion

# Expression-level preferences
dotnet_style_object_initializer = true:suggestion
dotnet_style_collection_initializer = true:suggestion
dotnet_style_explicit_tuple_names = true:suggestion
dotnet_style_null_propagation = true:suggestion
dotnet_style_coalesce_expression = true:suggestion
dotnet_style_prefer_is_null_check_over_reference_equality_method = true:suggestion
dotnet_style_prefer_inferred_tuple_names = true:suggestion
dotnet_style_prefer_inferred_anonymous_type_member_names = true:suggestion

# C# formatting rules
csharp_new_line_before_open_brace = all
csharp_new_line_before_else = true
csharp_new_line_before_catch = true
csharp_new_line_before_finally = true
csharp_new_line_before_members_in_object_initializers = true
csharp_new_line_before_members_in_anonymous_types = true
csharp_indent_case_contents = true
csharp_indent_switch_labels = true
csharp_space_around_binary_operators = before_and_after
"@
    
    $editorConfig | Out-File -FilePath $editorConfigPath -Encoding UTF8
    Write-Host "✓ Created .editorconfig" -ForegroundColor Green
}

# Create launch.json for VS Code debugging
$vscodeDir = ".vscode"
if (!(Test-Path $vscodeDir)) {
    New-Item -ItemType Directory -Path $vscodeDir -Force | Out-Null
}

$launchJsonPath = Join-Path $vscodeDir "launch.json"
if (!(Test-Path $launchJsonPath)) {
    $launchJson = @"
{
    "version": "0.2.0",
    "configurations": [
        {
            "name": "Launch MedicalAI.UI",
            "type": "coreclr",
            "request": "launch",
            "preLaunchTask": "build",
            "program": "`${workspaceFolder}/src/MedicalAI.UI/bin/Debug/net8.0/MedicalAI.UI.dll",
            "args": [],
            "cwd": "`${workspaceFolder}",
            "console": "internalConsole",
            "stopAtEntry": false
        },
        {
            "name": "Launch MedicalAI.CLI",
            "type": "coreclr",
            "request": "launch",
            "preLaunchTask": "build",
            "program": "`${workspaceFolder}/src/MedicalAI.CLI/bin/Debug/net8.0/MedicalAI.CLI.dll",
            "args": ["--help"],
            "cwd": "`${workspaceFolder}",
            "console": "internalConsole",
            "stopAtEntry": false
        },
        {
            "name": "Attach to Process",
            "type": "coreclr",
            "request": "attach"
        }
    ]
}
"@
    
    $launchJson | Out-File -FilePath $launchJsonPath -Encoding UTF8
    Write-Host "✓ Created VS Code launch.json" -ForegroundColor Green
}

# Create tasks.json for VS Code
$tasksJsonPath = Join-Path $vscodeDir "tasks.json"
if (!(Test-Path $tasksJsonPath)) {
    $tasksJson = @"
{
    "version": "2.0.0",
    "tasks": [
        {
            "label": "build",
            "command": "dotnet",
            "type": "process",
            "args": [
                "build",
                "`${workspaceFolder}/MedicalAI.ThesisSuite.sln"
            ],
            "problemMatcher": "`$msCompile",
            "group": {
                "kind": "build",
                "isDefault": true
            }
        },
        {
            "label": "test",
            "command": "dotnet",
            "type": "process",
            "args": [
                "test",
                "`${workspaceFolder}/MedicalAI.ThesisSuite.sln"
            ],
            "problemMatcher": "`$msCompile",
            "group": "test"
        },
        {
            "label": "clean",
            "command": "dotnet",
            "type": "process",
            "args": [
                "clean",
                "`${workspaceFolder}/MedicalAI.ThesisSuite.sln"
            ],
            "problemMatcher": "`$msCompile"
        },
        {
            "label": "restore",
            "command": "dotnet",
            "type": "process",
            "args": [
                "restore",
                "`${workspaceFolder}/MedicalAI.ThesisSuite.sln"
            ],
            "problemMatcher": "`$msCompile"
        },
        {
            "label": "publish-win",
            "command": "powershell",
            "type": "process",
            "args": [
                "-File",
                "`${workspaceFolder}/build/publish.ps1",
                "-Runtime",
                "win-x64"
            ],
            "problemMatcher": "`$msCompile",
            "group": "build"
        },
        {
            "label": "run-tests-with-coverage",
            "command": "powershell",
            "type": "process",
            "args": [
                "-File",
                "`${workspaceFolder}/build/test.ps1",
                "-Coverage"
            ],
            "problemMatcher": "`$msCompile",
            "group": "test"
        }
    ]
}
"@
    
    $tasksJson | Out-File -FilePath $tasksJsonPath -Encoding UTF8
    Write-Host "✓ Created VS Code tasks.json" -ForegroundColor Green
}

# Create settings.json for VS Code
$settingsJsonPath = Join-Path $vscodeDir "settings.json"
if (!(Test-Path $settingsJsonPath)) {
    $settingsJson = @"
{
    "dotnet.defaultSolution": "MedicalAI.ThesisSuite.sln",
    "files.exclude": {
        "**/bin": true,
        "**/obj": true,
        "**/.vs": true
    },
    "omnisharp.enableEditorConfigSupport": true,
    "omnisharp.enableRoslynAnalyzers": true,
    "editor.formatOnSave": true,
    "editor.codeActionsOnSave": {
        "source.organizeImports": "explicit"
    },
    "csharp.format.enable": true,
    "csharp.semanticHighlighting.enabled": true,
    "dotnet.completion.showCompletionItemsFromUnimportedNamespaces": true,
    "dotnet.inlayHints.enableInlayHintsForParameters": true,
    "dotnet.inlayHints.enableInlayHintsForLiteralParameters": true,
    "dotnet.inlayHints.enableInlayHintsForIndexerParameters": true,
    "dotnet.inlayHints.enableInlayHintsForObjectCreationParameters": true,
    "dotnet.inlayHints.enableInlayHintsForOtherParameters": true,
    "dotnet.inlayHints.suppressInlayHintsForParametersThatDifferOnlyBySuffix": true,
    "dotnet.inlayHints.suppressInlayHintsForParametersThatMatchMethodIntent": true,
    "dotnet.inlayHints.suppressInlayHintsForParametersThatMatchArgumentName": true
}
"@
    
    $settingsJson | Out-File -FilePath $settingsJsonPath -Encoding UTF8
    Write-Host "✓ Created VS Code settings.json" -ForegroundColor Green
}

# Create extensions.json for VS Code
$extensionsJsonPath = Join-Path $vscodeDir "extensions.json"
if (!(Test-Path $extensionsJsonPath)) {
    $extensionsJson = @"
{
    "recommendations": [
        "ms-dotnettools.csharp",
        "ms-dotnettools.csdevkit",
        "ms-dotnettools.vscode-dotnet-runtime",
        "ms-vscode.powershell",
        "editorconfig.editorconfig",
        "formulahendry.code-runner",
        "ryanluker.vscode-coverage-gutters",
        "ms-vscode.test-adapter-converter"
    ]
}
"@
    
    $extensionsJson | Out-File -FilePath $extensionsJsonPath -Encoding UTF8
    Write-Host "✓ Created VS Code extensions.json" -ForegroundColor Green
}

# Setup Git hooks if requested
if ($SetupGit) {
    Write-Host "Setting up Git hooks..." -ForegroundColor Yellow
    
    $gitHooksDir = ".git/hooks"
    if (Test-Path ".git") {
        # Pre-commit hook
        $preCommitHook = @"
#!/bin/sh
# Pre-commit hook for MedicalAI Thesis Suite

echo "Running pre-commit checks..."

# Check for large files
find . -size +50M -not -path "./.git/*" -not -path "./models/*" -not -path "./datasets/*" | while read file; do
    echo "Warning: Large file detected: `$file"
done

# Run code formatting
if command -v dotnet >/dev/null 2>&1; then
    echo "Formatting code..."
    dotnet format --no-restore --verbosity quiet
fi

# Run basic build check
echo "Running build check..."
dotnet build --no-restore --verbosity quiet
if [ `$? -ne 0 ]; then
    echo "Build failed. Commit aborted."
    exit 1
fi

echo "Pre-commit checks passed."
"@
        
        $preCommitPath = Join-Path $gitHooksDir "pre-commit"
        $preCommitHook | Out-File -FilePath $preCommitPath -Encoding UTF8
        Write-Host "✓ Created Git pre-commit hook" -ForegroundColor Green
    }
}

# Create development documentation
$devDocsPath = "docs/development-setup.md"
if (!(Test-Path $devDocsPath)) {
    $devDocs = @"
# Development Environment Setup

This document describes how to set up a development environment for the MedicalAI Thesis Suite project.

## Prerequisites

- .NET 8.0 SDK or later
- Git
- Visual Studio 2022, VS Code, or JetBrains Rider
- Windows 10/11, macOS 10.15+, or Linux (Ubuntu 20.04+)

## Quick Setup

Run the development setup script:

### Windows (PowerShell)
``````powershell
./build/setup-dev.ps1 -InstallTools -SetupGit
``````

### Linux/macOS (Bash)
``````bash
./build/setup-dev.sh --install-tools --setup-git
``````

## Manual Setup

### 1. Clone the Repository
``````bash
git clone <repository-url>
cd MedicalAI.ThesisSuite
``````

### 2. Restore Dependencies
``````bash
dotnet restore
``````

### 3. Build the Solution
``````bash
dotnet build
``````

### 4. Run Tests
``````bash
dotnet test
``````

## Development Tools

### Recommended Global Tools
``````bash
# Entity Framework Core tools
dotnet tool install -g dotnet-ef

# Code coverage report generator
dotnet tool install -g dotnet-reportgenerator-globaltool

# Check for outdated packages
dotnet tool install -g dotnet-outdated-tool

# Code formatter
dotnet tool install -g dotnet-format
``````

### VS Code Extensions
- C# Dev Kit
- C# Extensions
- PowerShell
- EditorConfig for VS Code
- Coverage Gutters

## Build Scripts

### Build for Current Platform
``````bash
# Windows
./build/publish.ps1

# Linux/macOS
./build/publish.sh
``````

### Build for All Platforms
``````bash
# Windows
./build/build-all.ps1

# Linux/macOS
./build/build-all.sh
``````

### Run Tests with Coverage
``````bash
# Windows
./build/test.ps1 -Coverage

# Linux/macOS
./build/test.sh --coverage
``````

## Debugging

### Visual Studio Code
1. Open the project folder in VS Code
2. Install recommended extensions
3. Use F5 to start debugging
4. Breakpoints and debugging features are configured automatically

### Visual Studio 2022
1. Open `MedicalAI.ThesisSuite.sln`
2. Set `MedicalAI.UI` as startup project
3. Use F5 to start debugging

## Code Quality

### Formatting
Code formatting is enforced through `.editorconfig`. Run:
``````bash
dotnet format
``````

### Static Analysis
The project uses built-in Roslyn analyzers. Check for issues:
``````bash
dotnet build --verbosity normal
``````

## Troubleshooting

### Common Issues

1. **Build Errors**: Ensure .NET 8.0 SDK is installed
2. **Missing Dependencies**: Run `dotnet restore`
3. **Test Failures**: Check test output for specific errors
4. **Performance Issues**: Use profiling tools in your IDE

### Getting Help

- Check the [Troubleshooting Guide](troubleshooting.md)
- Review build logs in the `dist` directory
- Check GitHub Issues for known problems
"@
    
    $devDocs | Out-File -FilePath $devDocsPath -Encoding UTF8
    Write-Host "✓ Created development setup documentation" -ForegroundColor Green
}

Write-Host "`n=== Development Environment Setup Complete ===" -ForegroundColor Green
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "1. Open the project in your preferred IDE" -ForegroundColor White
Write-Host "2. Install recommended extensions (VS Code)" -ForegroundColor White
Write-Host "3. Run 'dotnet build' to verify setup" -ForegroundColor White
Write-Host "4. Run 'dotnet test' to verify tests" -ForegroundColor White
Write-Host "5. Review docs/development-setup.md for detailed information" -ForegroundColor White
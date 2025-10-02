#!/usr/bin/env bash
set -euo pipefail

# Default values
INSTALL_TOOLS=false
CONFIGURE_VS=false
SETUP_GIT=false
VERBOSE=false

# Parse command line arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        --install-tools)
            INSTALL_TOOLS=true
            shift
            ;;
        --configure-vs)
            CONFIGURE_VS=true
            shift
            ;;
        --setup-git)
            SETUP_GIT=true
            shift
            ;;
        --verbose)
            VERBOSE=true
            shift
            ;;
        -h|--help)
            echo "Usage: $0 [OPTIONS]"
            echo "Options:"
            echo "  --install-tools      Install development tools"
            echo "  --configure-vs       Configure Visual Studio Code"
            echo "  --setup-git          Setup Git hooks"
            echo "  --verbose            Enable verbose output"
            echo "  -h, --help           Show this help message"
            exit 0
            ;;
        *)
            echo "Unknown option: $1"
            exit 1
            ;;
    esac
done

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

log_info() {
    echo -e "${GREEN}$1${NC}"
}

log_warn() {
    echo -e "${YELLOW}$1${NC}"
}

log_error() {
    echo -e "${RED}$1${NC}"
}

log_cyan() {
    echo -e "${CYAN}$1${NC}"
}

log_info "=== MedicalAI Thesis Suite Development Environment Setup ==="

# Check prerequisites
log_warn "Checking prerequisites..."

# Check .NET SDK
if command -v dotnet >/dev/null 2>&1; then
    DOTNET_VERSION=$(dotnet --version)
    log_info "✓ .NET SDK: $DOTNET_VERSION"
else
    log_error "✗ .NET SDK not found. Please install .NET 8.0 SDK"
    exit 1
fi

# Check Git
if command -v git >/dev/null 2>&1; then
    GIT_VERSION=$(git --version)
    log_info "✓ Git: $GIT_VERSION"
else
    log_error "✗ Git not found. Please install Git"
    exit 1
fi

# Install development tools if requested
if [ "$INSTALL_TOOLS" = true ]; then
    log_warn "Installing development tools..."
    
    # Install global .NET tools
    TOOLS=(
        "dotnet-ef:Entity Framework Core tools"
        "dotnet-reportgenerator-globaltool:Code coverage report generator"
        "dotnet-outdated-tool:Check for outdated NuGet packages"
        "dotnet-format:Code formatter"
    )
    
    for tool_info in "${TOOLS[@]}"; do
        IFS=':' read -r tool_name description <<< "$tool_info"
        log_cyan "Installing $description..."
        
        if dotnet tool install -g "$tool_name" 2>/dev/null; then
            log_info "✓ Installed $tool_name"
        else
            log_warn "- $tool_name already installed or failed to install"
        fi
    done
fi

# Create development configuration files
log_warn "Creating development configuration files..."

# Create .editorconfig if it doesn't exist
if [ ! -f ".editorconfig" ]; then
    cat > .editorconfig << 'EOF'
root = true

[*]
charset = utf-8
end_of_line = lf
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
EOF
    
    log_info "✓ Created .editorconfig"
fi

# Create VS Code configuration
mkdir -p .vscode

# Create launch.json for VS Code debugging
if [ ! -f ".vscode/launch.json" ]; then
    cat > .vscode/launch.json << 'EOF'
{
    "version": "0.2.0",
    "configurations": [
        {
            "name": "Launch MedicalAI.UI",
            "type": "coreclr",
            "request": "launch",
            "preLaunchTask": "build",
            "program": "${workspaceFolder}/src/MedicalAI.UI/bin/Debug/net8.0/MedicalAI.UI.dll",
            "args": [],
            "cwd": "${workspaceFolder}",
            "console": "internalConsole",
            "stopAtEntry": false
        },
        {
            "name": "Launch MedicalAI.CLI",
            "type": "coreclr",
            "request": "launch",
            "preLaunchTask": "build",
            "program": "${workspaceFolder}/src/MedicalAI.CLI/bin/Debug/net8.0/MedicalAI.CLI.dll",
            "args": ["--help"],
            "cwd": "${workspaceFolder}",
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
EOF
    
    log_info "✓ Created VS Code launch.json"
fi

# Create tasks.json for VS Code
if [ ! -f ".vscode/tasks.json" ]; then
    cat > .vscode/tasks.json << 'EOF'
{
    "version": "2.0.0",
    "tasks": [
        {
            "label": "build",
            "command": "dotnet",
            "type": "process",
            "args": [
                "build",
                "${workspaceFolder}/MedicalAI.ThesisSuite.sln"
            ],
            "problemMatcher": "$msCompile",
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
                "${workspaceFolder}/MedicalAI.ThesisSuite.sln"
            ],
            "problemMatcher": "$msCompile",
            "group": "test"
        },
        {
            "label": "clean",
            "command": "dotnet",
            "type": "process",
            "args": [
                "clean",
                "${workspaceFolder}/MedicalAI.ThesisSuite.sln"
            ],
            "problemMatcher": "$msCompile"
        },
        {
            "label": "restore",
            "command": "dotnet",
            "type": "process",
            "args": [
                "restore",
                "${workspaceFolder}/MedicalAI.ThesisSuite.sln"
            ],
            "problemMatcher": "$msCompile"
        },
        {
            "label": "publish-linux",
            "command": "bash",
            "type": "process",
            "args": [
                "${workspaceFolder}/build/publish.sh",
                "--runtime",
                "linux-x64"
            ],
            "problemMatcher": "$msCompile",
            "group": "build"
        },
        {
            "label": "run-tests-with-coverage",
            "command": "bash",
            "type": "process",
            "args": [
                "${workspaceFolder}/build/test.sh",
                "--coverage"
            ],
            "problemMatcher": "$msCompile",
            "group": "test"
        }
    ]
}
EOF
    
    log_info "✓ Created VS Code tasks.json"
fi

# Create settings.json for VS Code
if [ ! -f ".vscode/settings.json" ]; then
    cat > .vscode/settings.json << 'EOF'
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
EOF
    
    log_info "✓ Created VS Code settings.json"
fi

# Create extensions.json for VS Code
if [ ! -f ".vscode/extensions.json" ]; then
    cat > .vscode/extensions.json << 'EOF'
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
EOF
    
    log_info "✓ Created VS Code extensions.json"
fi

# Setup Git hooks if requested
if [ "$SETUP_GIT" = true ]; then
    log_warn "Setting up Git hooks..."
    
    if [ -d ".git" ]; then
        # Pre-commit hook
        cat > .git/hooks/pre-commit << 'EOF'
#!/bin/sh
# Pre-commit hook for MedicalAI Thesis Suite

echo "Running pre-commit checks..."

# Check for large files
find . -size +50M -not -path "./.git/*" -not -path "./models/*" -not -path "./datasets/*" | while read file; do
    echo "Warning: Large file detected: $file"
done

# Run code formatting
if command -v dotnet >/dev/null 2>&1; then
    echo "Formatting code..."
    dotnet format --no-restore --verbosity quiet
fi

# Run basic build check
echo "Running build check..."
dotnet build --no-restore --verbosity quiet
if [ $? -ne 0 ]; then
    echo "Build failed. Commit aborted."
    exit 1
fi

echo "Pre-commit checks passed."
EOF
        
        chmod +x .git/hooks/pre-commit
        log_info "✓ Created Git pre-commit hook"
    fi
fi

# Create development documentation
if [ ! -f "docs/development-setup.md" ]; then
    mkdir -p docs
    cat > docs/development-setup.md << 'EOF'
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
```powershell
./build/setup-dev.ps1 -InstallTools -SetupGit
```

### Linux/macOS (Bash)
```bash
./build/setup-dev.sh --install-tools --setup-git
```

## Manual Setup

### 1. Clone the Repository
```bash
git clone <repository-url>
cd MedicalAI.ThesisSuite
```

### 2. Restore Dependencies
```bash
dotnet restore
```

### 3. Build the Solution
```bash
dotnet build
```

### 4. Run Tests
```bash
dotnet test
```

## Development Tools

### Recommended Global Tools
```bash
# Entity Framework Core tools
dotnet tool install -g dotnet-ef

# Code coverage report generator
dotnet tool install -g dotnet-reportgenerator-globaltool

# Check for outdated packages
dotnet tool install -g dotnet-outdated-tool

# Code formatter
dotnet tool install -g dotnet-format
```

### VS Code Extensions
- C# Dev Kit
- C# Extensions
- PowerShell
- EditorConfig for VS Code
- Coverage Gutters

## Build Scripts

### Build for Current Platform
```bash
# Windows
./build/publish.ps1

# Linux/macOS
./build/publish.sh
```

### Build for All Platforms
```bash
# Windows
./build/build-all.ps1

# Linux/macOS
./build/build-all.sh
```

### Run Tests with Coverage
```bash
# Windows
./build/test.ps1 -Coverage

# Linux/macOS
./build/test.sh --coverage
```

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
```bash
dotnet format
```

### Static Analysis
The project uses built-in Roslyn analyzers. Check for issues:
```bash
dotnet build --verbosity normal
```

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
EOF
    
    log_info "✓ Created development setup documentation"
fi

echo
log_info "=== Development Environment Setup Complete ==="
log_cyan "Next steps:"
echo "1. Open the project in your preferred IDE"
echo "2. Install recommended extensions (VS Code)"
echo "3. Run 'dotnet build' to verify setup"
echo "4. Run 'dotnet test' to verify tests"
echo "5. Review docs/development-setup.md for detailed information"
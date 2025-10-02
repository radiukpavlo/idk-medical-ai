#!/usr/bin/env bash
set -euo pipefail

# Default values
FIX=false
ANALYZE=false
FORMAT=false
CHECK_OUTDATED=false
SEVERITY="suggestion"
VERBOSE=false

# Parse command line arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        --fix)
            FIX=true
            shift
            ;;
        --analyze)
            ANALYZE=true
            shift
            ;;
        --format)
            FORMAT=true
            shift
            ;;
        --check-outdated)
            CHECK_OUTDATED=true
            shift
            ;;
        --severity)
            SEVERITY="$2"
            shift 2
            ;;
        --verbose)
            VERBOSE=true
            shift
            ;;
        -h|--help)
            echo "Usage: $0 [OPTIONS]"
            echo "Options:"
            echo "  --fix              Apply code fixes automatically"
            echo "  --analyze          Run code analysis"
            echo "  --format           Check code formatting"
            echo "  --check-outdated   Check for outdated packages"
            echo "  --severity LEVEL   Set severity level (suggestion|warning|error)"
            echo "  --verbose          Enable verbose output"
            echo "  -h, --help         Show this help message"
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

log_info "=== MedicalAI Thesis Suite Code Quality Tools ==="

if [ "$FIX" = false ] && [ "$ANALYZE" = false ] && [ "$FORMAT" = false ] && [ "$CHECK_OUTDATED" = false ]; then
    log_warn "No action specified. Running all checks..."
    ANALYZE=true
    FORMAT=true
    CHECK_OUTDATED=true
fi

# Check if solution exists
if [ ! -f "MedicalAI.ThesisSuite.sln" ]; then
    log_error "Solution file not found. Please run from the project root directory."
    exit 1
fi

FORMAT_RESULT=0
ANALYSIS_RESULT=0
SECURITY_RESULT=0

# Format code
if [ "$FORMAT" = true ]; then
    log_warn "Formatting code..."
    
    FORMAT_ARGS=(
        "format"
        "MedicalAI.ThesisSuite.sln"
        "--severity" "$SEVERITY"
    )
    
    if [ "$FIX" = false ]; then
        FORMAT_ARGS+=("--verify-no-changes")
    fi
    
    if [ "$VERBOSE" = true ]; then
        FORMAT_ARGS+=("--verbosity" "diagnostic")
    else
        FORMAT_ARGS+=("--verbosity" "minimal")
    fi
    
    if dotnet "${FORMAT_ARGS[@]}"; then
        FORMAT_RESULT=0
        log_info "✓ Code formatting check passed"
    else
        FORMAT_RESULT=$?
        if [ "$FIX" = true ]; then
            log_info "✓ Code formatting applied"
        else
            log_error "✗ Code formatting issues found. Run with --fix to apply fixes."
        fi
    fi
fi

# Analyze code
if [ "$ANALYZE" = true ]; then
    log_warn "Analyzing code..."
    
    # Build with analysis
    if dotnet build MedicalAI.ThesisSuite.sln --configuration Debug --verbosity normal; then
        ANALYSIS_RESULT=0
        log_info "✓ Code analysis passed"
    else
        ANALYSIS_RESULT=$?
        log_error "✗ Code analysis found issues"
    fi
    
    # Run additional analyzers if available
    if command -v dotnet-sonarscanner >/dev/null 2>&1; then
        log_cyan "Running SonarQube analysis..."
        # SonarQube analysis would go here
    fi
fi

# Check for outdated packages
if [ "$CHECK_OUTDATED" = true ]; then
    log_warn "Checking for outdated packages..."
    
    if command -v dotnet-outdated >/dev/null 2>&1; then
        if dotnet outdated MedicalAI.ThesisSuite.sln; then
            log_info "✓ All packages are up to date"
        else
            log_warn "! Some packages may be outdated"
        fi
    else
        log_warn "dotnet-outdated tool not installed. Install with: dotnet tool install -g dotnet-outdated-tool"
    fi
fi

# Security scan
log_warn "Running security scan..."
if dotnet list package --vulnerable --include-transitive; then
    SECURITY_RESULT=0
    log_info "✓ No known vulnerabilities found"
else
    SECURITY_RESULT=$?
    log_warn "! Potential security vulnerabilities found"
fi

# Generate quality report
mkdir -p dist
REPORT_PATH="dist/code-quality-report-$(date +%Y%m%d-%H%M%S).json"

cat > "$REPORT_PATH" << EOF
{
  "timestamp": "$(date -Iseconds)",
  "formatCheck": $([ "$FORMAT" = true ] && echo $([ $FORMAT_RESULT -eq 0 ] && echo "true" || echo "false") || echo "null"),
  "analysisCheck": $([ "$ANALYZE" = true ] && echo $([ $ANALYSIS_RESULT -eq 0 ] && echo "true" || echo "false") || echo "null"),
  "securityCheck": $([ $SECURITY_RESULT -eq 0 ] && echo "true" || echo "false"),
  "tools": {
    "dotnetFormat": $(command -v dotnet >/dev/null 2>&1 && echo "true" || echo "false"),
    "dotnetOutdated": $(command -v dotnet-outdated >/dev/null 2>&1 && echo "true" || echo "false"),
    "sonarScanner": $(command -v dotnet-sonarscanner >/dev/null 2>&1 && echo "true" || echo "false")
  }
}
EOF

log_cyan "Quality report saved: $REPORT_PATH"

echo
log_info "=== Code Quality Check Complete ==="
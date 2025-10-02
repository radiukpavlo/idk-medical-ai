#!/usr/bin/env bash
set -euo pipefail

# Default values
CONFIGURATION="Debug"
FILTER=""
COVERAGE=false
VERBOSE=false
LOGGER="console;verbosity=normal"
OUTPUT_PATH="./test-results"

# Parse command line arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        -c|--configuration)
            CONFIGURATION="$2"
            shift 2
            ;;
        -f|--filter)
            FILTER="$2"
            shift 2
            ;;
        --coverage)
            COVERAGE=true
            shift
            ;;
        --verbose)
            VERBOSE=true
            shift
            ;;
        --logger)
            LOGGER="$2"
            shift 2
            ;;
        -o|--output)
            OUTPUT_PATH="$2"
            shift 2
            ;;
        -h|--help)
            echo "Usage: $0 [OPTIONS]"
            echo "Options:"
            echo "  -c, --configuration   Build configuration (Debug|Release) [default: Debug]"
            echo "  -f, --filter         Test filter expression"
            echo "  --coverage           Enable code coverage collection"
            echo "  --verbose            Enable verbose output"
            echo "  --logger             Test logger configuration [default: console;verbosity=normal]"
            echo "  -o, --output         Output directory [default: ./test-results]"
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

log_info "=== MedicalAI Thesis Suite Test Runner ==="
log_cyan "Configuration: $CONFIGURATION"
log_cyan "Filter: $([ -n "$FILTER" ] && echo "$FILTER" || echo "All tests")"
log_cyan "Coverage: $COVERAGE"

# Create output directory
mkdir -p "$OUTPUT_PATH"

# Clean and restore
log_warn "Preparing test environment..."
dotnet clean --configuration "$CONFIGURATION" --verbosity minimal
dotnet restore --verbosity minimal

# Build solution
log_warn "Building solution..."
if ! dotnet build --configuration "$CONFIGURATION" --no-restore --verbosity minimal; then
    log_error "Build failed"
    exit 1
fi

# Prepare test arguments
TEST_ARGS=(
    "test"
    "--configuration" "$CONFIGURATION"
    "--no-build"
    "--logger" "$LOGGER"
    "--results-directory" "$OUTPUT_PATH"
)

if [ -n "$FILTER" ]; then
    TEST_ARGS+=("--filter" "$FILTER")
fi

if [ "$VERBOSE" = true ]; then
    TEST_ARGS+=("--verbosity" "detailed")
else
    TEST_ARGS+=("--verbosity" "normal")
fi

# Add coverage if requested
if [ "$COVERAGE" = true ]; then
    log_warn "Enabling code coverage..."
    TEST_ARGS+=("--collect" "XPlat Code Coverage")
    if [ -f "build/coverlet.runsettings" ]; then
        TEST_ARGS+=("--settings" "build/coverlet.runsettings")
    fi
fi

# Run tests
log_warn "Running tests..."
TEST_START_TIME=$(date +%s)

if dotnet "${TEST_ARGS[@]}"; then
    TEST_RESULT=0
    log_info "✓ All tests passed!"
else
    TEST_RESULT=$?
    log_error "✗ Some tests failed"
fi

TEST_END_TIME=$(date +%s)
TEST_DURATION=$((TEST_END_TIME - TEST_START_TIME))

log_cyan "Test duration: ${TEST_DURATION}s"

# Process coverage if enabled
if [ "$COVERAGE" = true ] && [ $TEST_RESULT -eq 0 ]; then
    log_warn "Processing coverage results..."
    
    # Find coverage files
    COVERAGE_FILES=$(find "$OUTPUT_PATH" -name "coverage.cobertura.xml" -type f)
    
    if [ -n "$COVERAGE_FILES" ]; then
        COVERAGE_COUNT=$(echo "$COVERAGE_FILES" | wc -l)
        log_cyan "Coverage files found: $COVERAGE_COUNT"
        
        # Try to generate HTML report if reportgenerator is available
        if command -v reportgenerator >/dev/null 2>&1; then
            HTML_OUTPUT_PATH="$OUTPUT_PATH/coverage-html"
            FIRST_COVERAGE_FILE=$(echo "$COVERAGE_FILES" | head -n1)
            reportgenerator -reports:"$FIRST_COVERAGE_FILE" -targetdir:"$HTML_OUTPUT_PATH" -reporttypes:Html
            log_cyan "HTML coverage report: $HTML_OUTPUT_PATH/index.html"
        else
            log_warn "Install ReportGenerator for HTML coverage reports: dotnet tool install -g dotnet-reportgenerator-globaltool"
        fi
    fi
fi

log_cyan "Test results saved to: $OUTPUT_PATH"

exit $TEST_RESULT
#!/usr/bin/env bash
set -euo pipefail

# Default values
CONFIGURATION="Release"
RUN_TESTS=false
VERBOSE=false
OUTPUT_PATH="./dist"

# Parse command line arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        -c|--configuration)
            CONFIGURATION="$2"
            shift 2
            ;;
        --run-tests)
            RUN_TESTS=true
            shift
            ;;
        --verbose)
            VERBOSE=true
            shift
            ;;
        -o|--output)
            OUTPUT_PATH="$2"
            shift 2
            ;;
        -h|--help)
            echo "Usage: $0 [OPTIONS]"
            echo "Options:"
            echo "  -c, --configuration   Build configuration (Debug|Release) [default: Release]"
            echo "  --run-tests          Run tests before publishing"
            echo "  --verbose            Enable verbose output"
            echo "  -o, --output         Output directory [default: ./dist]"
            echo "  -h, --help           Show this help message"
            exit 0
            ;;
        *)
            echo "Unknown option: $1"
            exit 1
            ;;
    esac
done

# Supported runtimes
RUNTIMES=(
    "linux-x64"
    "linux-arm64"
    "osx-x64"
    "osx-arm64"
    "win-x64"
    "win-arm64"
)

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

log_info "=== Building MedicalAI Thesis Suite for All Platforms ==="
log_cyan "Configuration: $CONFIGURATION"
log_cyan "Platforms: ${RUNTIMES[*]}"

BUILD_RESULTS=()
START_TIME=$(date +%s)

# Restore packages once
log_warn "Restoring NuGet packages..."
if ! dotnet restore --verbosity minimal; then
    log_error "Package restore failed"
    exit 1
fi

for runtime in "${RUNTIMES[@]}"; do
    echo
    log_warn "--- Building for $runtime ---"
    RUNTIME_START_TIME=$(date +%s)
    
    BUILD_ARGS=(
        --configuration "$CONFIGURATION"
        --runtime "$runtime"
        --output "$OUTPUT_PATH"
        --skip-restore
    )
    
    if [ "$runtime" = "linux-x64" ] && [ "$RUN_TESTS" = true ]; then
        # Only run tests once on the primary platform
        BUILD_ARGS+=(--run-tests)
    fi
    
    if [ "$VERBOSE" = true ]; then
        BUILD_ARGS+=(--verbose)
    fi
    
    if "$(dirname "$0")/publish.sh" "${BUILD_ARGS[@]}"; then
        RUNTIME_END_TIME=$(date +%s)
        BUILD_TIME=$((RUNTIME_END_TIME - RUNTIME_START_TIME))
        BUILD_RESULTS+=("$runtime:Success:${BUILD_TIME}s:")
        log_info "✓ $runtime completed in ${BUILD_TIME}s"
    else
        RUNTIME_END_TIME=$(date +%s)
        BUILD_TIME=$((RUNTIME_END_TIME - RUNTIME_START_TIME))
        BUILD_RESULTS+=("$runtime:Failed:${BUILD_TIME}s:Build failed")
        log_error "✗ $runtime failed"
    fi
done

END_TIME=$(date +%s)
TOTAL_TIME=$((END_TIME - START_TIME))

echo
log_info "=== Build Summary ==="
printf "%-12s %-8s %-10s %s\n" "Runtime" "Status" "Time" "Error"
printf "%-12s %-8s %-10s %s\n" "-------" "------" "----" "-----"

SUCCESS_COUNT=0
FAIL_COUNT=0

for result in "${BUILD_RESULTS[@]}"; do
    IFS=':' read -r runtime status time error <<< "$result"
    printf "%-12s %-8s %-10s %s\n" "$runtime" "$status" "$time" "$error"
    
    if [ "$status" = "Success" ]; then
        ((SUCCESS_COUNT++))
    else
        ((FAIL_COUNT++))
    fi
done

echo
log_cyan "Total builds: ${#BUILD_RESULTS[@]}"
log_info "Successful: $SUCCESS_COUNT"
log_error "Failed: $FAIL_COUNT"
log_cyan "Total time: ${TOTAL_TIME}s"

# Generate build report
REPORT_PATH="$OUTPUT_PATH/build-report-$(date +%Y%m%d-%H%M%S).json"
mkdir -p "$OUTPUT_PATH"

cat > "$REPORT_PATH" << EOF
{
  "buildDate": "$(date -Iseconds)",
  "configuration": "$CONFIGURATION",
  "totalTime": "${TOTAL_TIME}s",
  "results": [
EOF

FIRST=true
for result in "${BUILD_RESULTS[@]}"; do
    IFS=':' read -r runtime status time error <<< "$result"
    
    if [ "$FIRST" = true ]; then
        FIRST=false
    else
        echo "," >> "$REPORT_PATH"
    fi
    
    cat >> "$REPORT_PATH" << EOF
    {
      "runtime": "$runtime",
      "status": "$status",
      "buildTime": "$time",
      "error": "$error"
    }
EOF
done

cat >> "$REPORT_PATH" << EOF
  ],
  "summary": {
    "total": ${#BUILD_RESULTS[@]},
    "successful": $SUCCESS_COUNT,
    "failed": $FAIL_COUNT
  }
}
EOF

log_cyan "Build report saved: $REPORT_PATH"

if [ $FAIL_COUNT -gt 0 ]; then
    exit 1
fi
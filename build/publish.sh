#!/usr/bin/env bash
set -euo pipefail

# Default values
CONFIGURATION="Release"
RUNTIME="${1:-linux-x64}"
RUN_TESTS=false
SKIP_RESTORE=false
VERBOSE=false
OUTPUT_PATH="./dist"

# Parse command line arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        -c|--configuration)
            CONFIGURATION="$2"
            shift 2
            ;;
        -r|--runtime)
            RUNTIME="$2"
            shift 2
            ;;
        --run-tests)
            RUN_TESTS=true
            shift
            ;;
        --skip-restore)
            SKIP_RESTORE=true
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
            echo "  -r, --runtime         Target runtime [default: linux-x64]"
            echo "  --run-tests          Run tests before publishing"
            echo "  --skip-restore       Skip package restore"
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

log_info "=== MedicalAI Thesis Suite Build Script ==="
log_cyan "Configuration: $CONFIGURATION"
log_cyan "Runtime: $RUNTIME"
log_cyan "Run Tests: $RUN_TESTS"
log_cyan "Output Path: $OUTPUT_PATH"

# Create output directory
mkdir -p "$OUTPUT_PATH"
log_warn "Created output directory: $OUTPUT_PATH"

# Clean previous builds
log_warn "Cleaning previous builds..."
find src -type d \( -name "bin" -o -name "obj" \) -exec rm -rf {} + 2>/dev/null || true

# Restore packages
if [ "$SKIP_RESTORE" = false ]; then
    log_warn "Restoring NuGet packages..."
    if ! dotnet restore --verbosity minimal; then
        log_error "Package restore failed"
        exit 1
    fi
fi

# Build solution
log_warn "Building solution..."
if ! dotnet build --configuration "$CONFIGURATION" --no-restore --verbosity minimal; then
    log_error "Build failed"
    exit 1
fi

# Run tests if requested
if [ "$RUN_TESTS" = true ]; then
    log_warn "Running tests..."
    if ! dotnet test --configuration "$CONFIGURATION" --no-build --verbosity minimal --logger "console;verbosity=normal"; then
        log_warn "Some tests failed, but continuing with build"
    fi
fi

# Publish application
log_warn "Publishing application for $RUNTIME..."
PUBLISH_PATH="src/MedicalAI.UI/bin/$CONFIGURATION/net8.0/$RUNTIME/publish"
if ! dotnet publish ./src/MedicalAI.UI -c "$CONFIGURATION" -r "$RUNTIME" -p:PublishSingleFile=true -p:SelfContained=true --no-build; then
    log_error "Publish failed"
    exit 1
fi

# Copy models and datasets if they exist
if [ -d "models" ]; then
    log_warn "Copying AI models..."
    cp -r models "$PUBLISH_PATH/"
fi

if [ -d "datasets/samples" ]; then
    log_warn "Copying sample datasets..."
    mkdir -p "$PUBLISH_PATH/datasets"
    cp -r datasets/samples/* "$PUBLISH_PATH/datasets/"
fi

# Create distribution package
PACKAGE_NAME="MedicalAI.ThesisSuite-$RUNTIME-$(date +%Y%m%d)"
PACKAGE_PATH="$OUTPUT_PATH/$PACKAGE_NAME.tar.gz"

log_warn "Creating distribution package..."
(cd "$PUBLISH_PATH" && tar -czf "../../../../../../../$PACKAGE_PATH" .)

# Generate build info
BUILD_INFO_PATH="$OUTPUT_PATH/$PACKAGE_NAME-buildinfo.json"
cat > "$BUILD_INFO_PATH" << EOF
{
  "buildDate": "$(date -Iseconds)",
  "configuration": "$CONFIGURATION",
  "runtime": "$RUNTIME",
  "dotnetVersion": "$(dotnet --version)",
  "gitCommit": "$(git rev-parse --short HEAD 2>/dev/null || echo 'unknown')",
  "buildHost": "$(uname -a)"
}
EOF

log_info "=== Build Complete ==="
log_cyan "Package: $PACKAGE_PATH"
log_cyan "Build Info: $BUILD_INFO_PATH"
if command -v du >/dev/null 2>&1; then
    PACKAGE_SIZE=$(du -h "$PACKAGE_PATH" | cut -f1)
    log_cyan "Package Size: $PACKAGE_SIZE"
fi

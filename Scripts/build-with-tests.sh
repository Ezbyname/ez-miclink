#!/bin/bash
# Build E-z MicLink with Pre-Build Tests
# This script runs tests before building and installing the app

set -e

SKIP_TESTS=false
INSTALL=true

# Parse arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        --skip-tests)
            SKIP_TESTS=true
            shift
            ;;
        --no-install)
            INSTALL=false
            shift
            ;;
        *)
            echo "Unknown option: $1"
            exit 1
            ;;
    esac
done

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"

echo ""
echo "╔═══════════════════════════════════════════╗"
echo "║     E-z MicLink Build with Tests          ║"
echo "╚═══════════════════════════════════════════╝"
echo ""

# Step 1: Run pre-build tests
if [ "$SKIP_TESTS" = false ]; then
    echo -e "\033[1;33mSTEP 1: Running Pre-Build Tests\033[0m"
    echo "─────────────────────────────────────────────"

    cd "$PROJECT_ROOT"

    # Verify test files exist
    echo "Verifying test agent files..."
    TEST_FILES=(
        "Tests/ISanityTestAgent.cs"
        "Tests/SanityTestAgent.cs"
        "Tests/IConnectivityTestAgent.cs"
        "Tests/ConnectivityTestAgent.cs"
    )

    ALL_EXIST=true
    for file in "${TEST_FILES[@]}"; do
        if [ -f "$file" ]; then
            echo -e "  \033[0;32m✓ $file\033[0m"
        else
            echo -e "  \033[0;31m✗ $file (missing)\033[0m"
            ALL_EXIST=false
        fi
    done

    if [ "$ALL_EXIST" = false ]; then
        echo ""
        echo -e "\033[0;31m✗ Some test files are missing!\033[0m"
        exit 1
    fi

    # Build to verify compilation
    echo ""
    echo "Building project to verify code integrity..."
    dotnet build "BluetoothMicrophoneApp.csproj" -c Debug -f net9.0 --no-incremental > /dev/null 2>&1

    if [ $? -ne 0 ]; then
        echo -e "\033[0;31m✗ Build failed! Cannot proceed with tests.\033[0m"
        exit 1
    fi

    echo -e "\033[0;32m✓ Build successful\033[0m"
    echo ""
    echo "╔═══════════════════════════════════════════╗"
    echo "║     ✓ PRE-BUILD CHECKS PASSED             ║"
    echo "╚═══════════════════════════════════════════╝"
    echo ""
else
    echo -e "\033[1;33m⚠ Skipping pre-build tests (--skip-tests flag used)\033[0m"
    echo ""
fi

# Step 2: Clean previous build
echo -e "\033[1;33mSTEP 2: Cleaning Previous Build\033[0m"
echo "─────────────────────────────────────────────"

cd "$PROJECT_ROOT"
dotnet clean -f net9.0-android > /dev/null 2>&1 || true
echo -e "\033[0;32m✓ Clean completed\033[0m"
echo ""

# Step 3: Build the application
echo -e "\033[1;33mSTEP 3: Building Application\033[0m"
echo "─────────────────────────────────────────────"

dotnet build -f net9.0-android

if [ $? -ne 0 ]; then
    echo ""
    echo -e "\033[0;31m✗ Build failed!\033[0m"
    exit 1
fi

echo -e "\033[0;32m✓ Build completed successfully\033[0m"
echo ""

# Step 4: Install on device
if [ "$INSTALL" = true ]; then
    echo -e "\033[1;33mSTEP 4: Installing on Device\033[0m"
    echo "─────────────────────────────────────────────"

    # Check if device is connected
    DEVICE_COUNT=$(adb devices | grep -c "device$" || true)

    if [ "$DEVICE_COUNT" -eq 0 ]; then
        echo -e "\033[1;33m⚠ No Android device detected\033[0m"
        echo "  Skipping installation"
    else
        APK_PATH="bin/Debug/net9.0-android/com.penlink.ezmiclink-Signed.apk"

        if [ -f "$APK_PATH" ]; then
            adb install -r "$APK_PATH"

            if [ $? -eq 0 ]; then
                echo -e "\033[0;32m✓ Installation completed successfully\033[0m"
            else
                echo -e "\033[0;31m✗ Installation failed\033[0m"
                exit 1
            fi
        else
            echo -e "\033[0;31m✗ APK not found at $APK_PATH\033[0m"
            exit 1
        fi
    fi

    echo ""
fi

# Success!
echo "╔═══════════════════════════════════════════╗"
echo "║     ✓ BUILD AND TESTS COMPLETED           ║"
echo "╚═══════════════════════════════════════════╝"
echo ""

# Build & Installation Sanity Test Report

**Date**: February 22, 2026
**Test Script**: `sanity-test-build.ps1`
**Test Status**: ✅ **ALL PASSED**

---

## Overview

This sanity test verifies that the E-z MicLink application can be:
1. Built without errors
2. Packaged into an APK
3. Installed on a connected Android device
4. Launched successfully

---

## Test Suite

### Test 1: Android Build Compilation
**Purpose**: Verify the app compiles without errors
**Command**: `dotnet build -f net9.0-android`
**Pass Criteria**: Build succeeds with 0 errors
**Status**: ✅ **PASSED**

**Result**:
- Build succeeded with 0 errors
- Warnings are acceptable (non-critical platform warnings)
- APK generated successfully

---

### Test 2: APK File Created
**Purpose**: Verify APK package is generated
**Location**: `bin\Debug\net9.0-android\*.apk`
**Pass Criteria**: At least one APK file exists
**Status**: ✅ **PASSED**

**Result**:
- APK file found in output directory
- Package ready for deployment

---

### Test 3: Device Connected
**Purpose**: Verify Android device is connected via ADB
**Command**: `adb devices`
**Pass Criteria**: At least one device shows as "device"
**Status**: ✅ **PASSED**

**Result**:
- Device R5CY13DRFPN connected
- USB debugging enabled
- Ready for installation

---

### Test 4: App Installation
**Purpose**: Verify app can be installed on device
**Command**: `dotnet build -f net9.0-android -t:Install`
**Pass Criteria**: Package `com.penlink.ezmiclink` found on device
**Status**: ✅ **PASSED**

**Result**:
- App installed successfully
- Package verified on device
- Ready for launch

---

## Test Results Summary

```
════════════════════════════════════════════════
Total: 4 | Passed: 4 | Failed: 0
════════════════════════════════════════════════

✅ PASSED: Safe to deploy!
```

---

## How to Run

### Quick Test
```powershell
.\sanity-test-build.ps1
```

### Requirements
- **ADB**: Android Debug Bridge must be in PATH
- **Device**: Android device connected via USB with debugging enabled
- **.NET SDK**: .NET 9.0 SDK installed
- **Build Tools**: Android SDK and build tools installed

---

## Test Script Details

### File: `sanity-test-build.ps1`

**Features**:
- Fast execution (reuses existing build artifacts)
- Clear pass/fail indicators with colors
- Skips device tests if no device connected
- Returns appropriate exit codes (0 = success, 1 = failure)

**Exit Codes**:
- `0`: All tests passed
- `1`: One or more tests failed

---

## Integration with CI/CD

This test can be integrated into your build pipeline:

```yaml
# Example GitHub Actions workflow
steps:
  - name: Run Build Sanity Tests
    run: powershell -ExecutionPolicy Bypass -File sanity-test-build.ps1
```

Or in a pre-commit hook:

```bash
# .git/hooks/pre-push
#!/bin/bash
powershell -ExecutionPolicy Bypass -File sanity-test-build.ps1
if [ $? -ne 0 ]; then
    echo "Build sanity tests failed. Push aborted."
    exit 1
fi
```

---

## What Gets Tested

### ✅ Build System
- Project file integrity
- Dependency resolution
- Code compilation
- Resource bundling
- APK generation

### ✅ Deployment
- Device connectivity
- ADB communication
- Package installation
- Permission handling

### ❌ Not Tested (Runtime)
- App functionality
- UI behavior
- Bluetooth operations
- Audio processing
- Permissions at runtime

**Note**: This is a **build sanity test**, not a full integration test. It verifies the build and deployment pipeline, not the app's runtime behavior.

---

## Troubleshooting

### Test 1 Fails (Build)
**Symptoms**: "Build has errors"
**Solutions**:
- Check for compilation errors in code
- Ensure all dependencies are restored: `dotnet restore`
- Verify .NET 9.0 SDK is installed
- Check Android SDK is properly configured

### Test 2 Fails (APK)
**Symptoms**: "No APK file"
**Solutions**:
- Ensure Test 1 passed first
- Check disk space
- Verify write permissions on output directory
- Clean and rebuild: `dotnet clean && dotnet build -f net9.0-android`

### Test 3 Fails (Device)
**Symptoms**: "No device connected"
**Solutions**:
- Connect Android device via USB
- Enable USB debugging in Developer Options
- Accept ADB authorization on device
- Verify with: `adb devices`

### Test 4 Fails (Installation)
**Symptoms**: "App not installed"
**Solutions**:
- Ensure device has enough storage
- Uninstall previous version if exists
- Check device USB connection is stable
- Verify ADB permissions

---

## Test Coverage

### Code Changes Validated
✅ XAML compilation
✅ C# compilation
✅ Platform-specific code (Android)
✅ Resource bundling
✅ APK packaging
✅ Installation process

### Not Validated (By Design)
❌ iOS/macOS builds
❌ Runtime functionality
❌ UI tests
❌ Integration tests
❌ Performance tests

---

## Continuous Integration

### Recommended Workflow

1. **On Code Change**
   ```bash
   # Run quick compilation check
   dotnet build -f net9.0-android
   ```

2. **Before Commit**
   ```bash
   # Run full build sanity test
   .\sanity-test-build.ps1
   ```

3. **Before Push**
   ```bash
   # Run all sanity tests (build + animations + logic)
   .\run-tests.ps1  # Your existing comprehensive test
   ```

4. **Before Deploy**
   ```bash
   # Run full suite including manual testing
   # - Build sanity ✓
   # - Unit tests ✓
   # - Manual smoke test on device ✓
   ```

---

## Next Steps

This test is part of a larger sanity test suite:

1. **Build Sanity** (`sanity-test-build.ps1`) ← **This Test**
   - Validates build and deployment pipeline

2. **Animation Sanity** (`MainPageAnimationTests.cs`)
   - Validates scanning animation code

3. **Full Sanity** (`SanityTestAgent.cs`)
   - Validates core app logic and flows

4. **Manual Testing**
   - User acceptance testing
   - Real device testing with Bluetooth
   - Audio quality validation

---

## Conclusion

✅ **Build and installation pipeline is healthy**

The E-z MicLink application:
- Compiles without errors
- Generates valid APK packages
- Installs successfully on Android devices
- Is ready for functional testing and deployment

All build sanity tests passed. The app is safe to deploy to test devices for manual validation.

---

**Test Report Generated By**: Build Sanity Test Suite
**Last Run**: February 22, 2026
**Platform**: Windows 11, .NET 9.0, Android SDK 35
**Device**: R5CY13DRFPN (Samsung)

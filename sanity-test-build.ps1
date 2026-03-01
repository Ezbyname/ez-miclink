Write-Host ""
Write-Host "===============================================" -ForegroundColor Cyan
Write-Host " SANITY TEST SUITE" -ForegroundColor Cyan
Write-Host "===============================================" -ForegroundColor Cyan
Write-Host ""

$totalPassed = 0
$totalFailed = 0

# ============================================================================
# NOTE: Comprehensive unit tests (SanityTestAgent.cs with 16 tests) are
# currently disabled due to MAUI project structure complexity. These tests
# verify AudioEngine, effects, auth flow, and device management.
# TODO: Refactor to class library structure to enable unit tests
# ============================================================================

# ============================================================================
# BUILD AND DEPLOYMENT TESTS
# ============================================================================

# Test 1: Build
Write-Host "Test 1: Android Build..." -ForegroundColor Yellow
$buildOutput = dotnet build -f net9.0-android 2>&1 | Out-String
if ($buildOutput -match "Build succeeded" -and $buildOutput -match "0 Error\(s\)") {
    Write-Host "  PASSED: Build succeeded with 0 errors" -ForegroundColor Green
    $totalPassed++
} else {
    Write-Host "  FAILED: Build has errors" -ForegroundColor Red
    $totalFailed++
}
Write-Host ""

# Test 2: APK exists
Write-Host "Test 2: APK File..." -ForegroundColor Yellow
$apkFiles = Get-ChildItem -Path "bin\Debug\net9.0-android" -Filter "*.apk" -Recurse -ErrorAction SilentlyContinue
if ($apkFiles.Count -gt 0) {
    Write-Host "  PASSED: APK file found" -ForegroundColor Green
    $totalPassed++
} else {
    Write-Host "  FAILED: No APK file" -ForegroundColor Red
    $totalFailed++
}
Write-Host ""

# Test 3: Device
Write-Host "Test 3: Device Connection..." -ForegroundColor Yellow
$devices = adb devices 2>&1 | Select-String "\tdevice$"
if ($devices) {
    Write-Host "  PASSED: Device connected" -ForegroundColor Green
    $totalPassed++

    # Test 4: Installation (if device available)
    Write-Host ""
    Write-Host "Test 4: Installation..." -ForegroundColor Yellow
    dotnet build -f net9.0-android -t:Install 2>&1 | Out-Null
    $installed = adb shell pm list packages 2>&1 | Select-String "com.penlink.ezmiclink"
    if ($installed) {
        Write-Host "  PASSED: App installed" -ForegroundColor Green
        $totalPassed++
    } else {
        Write-Host "  FAILED: App not installed" -ForegroundColor Red
        $totalFailed++
    }
} else {
    Write-Host "  SKIPPED: No device connected" -ForegroundColor DarkGray
}

# Summary
Write-Host ""
Write-Host "===============================================" -ForegroundColor Cyan
Write-Host "Total: $($totalPassed + $totalFailed) | Passed: $totalPassed | Failed: $totalFailed" -ForegroundColor Cyan
Write-Host "===============================================" -ForegroundColor Cyan

if ($totalFailed -gt 0) {
    Write-Host ""
    Write-Host "FAILED: Fix issues before deploying" -ForegroundColor Red
    exit 1
} else {
    Write-Host ""
    Write-Host "PASSED: Safe to deploy!" -ForegroundColor Green
    exit 0
}

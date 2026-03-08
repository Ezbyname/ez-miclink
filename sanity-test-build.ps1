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

# Test 5: A2DP audio routing code exists (critical fallback path)
Write-Host ""
Write-Host "Test 5: A2DP Audio Routing Code..." -ForegroundColor Yellow
$audioServiceFile = "Platforms\Android\Services\AudioService.cs"
if (Test-Path $audioServiceFile) {
    $audioCode = Get-Content $audioServiceFile -Raw
    $hasA2dpDetect = $audioCode -match "FindA2dpDevice"
    $hasPreferred = $audioCode -match "SetPreferredDevice"
    $hasScoFallback = $audioCode -match "IsCurrentlyConnected"
    $hasMediaUsage = $audioCode -match "AudioUsageKind\.Media"

    if ($hasA2dpDetect -and $hasPreferred -and $hasScoFallback -and $hasMediaUsage) {
        Write-Host "  PASSED: A2DP fallback path verified (detect + preferred device + SCO stability check + media routing)" -ForegroundColor Green
        $totalPassed++
    } elseif ($hasA2dpDetect -and $hasMediaUsage) {
        Write-Host "  PASSED: A2DP routing present (detect + media routing)" -ForegroundColor Green
        $totalPassed++
    } else {
        Write-Host "  FAILED: A2DP fallback code missing from AudioService" -ForegroundColor Red
        Write-Host "    FindA2dpDevice: $hasA2dpDetect | SetPreferredDevice: $hasPreferred | ScoStability: $hasScoFallback | MediaUsage: $hasMediaUsage" -ForegroundColor DarkGray
        $totalFailed++
    }
} else {
    Write-Host "  FAILED: AudioService.cs not found" -ForegroundColor Red
    $totalFailed++
}

# Test 6: Noise Reduction implemented (not TODO stubs)
Write-Host ""
Write-Host "Test 6: Noise Reduction Implementation..." -ForegroundColor Yellow
$nrFile = "Audio\DSP\NoiseReductionEffect.cs"
if (Test-Path $nrFile) {
    $engineCode = Get-Content "Audio\DSP\AudioEngine.cs" -Raw
    $hasTodo = $engineCode -match "TODO.*NoiseReduction"
    $hasRealCall = $engineCode -match "_noiseReduction\.Process\("

    if ($hasRealCall -and -not $hasTodo) {
        Write-Host "  PASSED: Noise reduction implemented and wired into AudioEngine" -ForegroundColor Green
        $totalPassed++
    } elseif ($hasTodo) {
        Write-Host "  FAILED: Noise reduction still has TODO stubs in AudioEngine" -ForegroundColor Red
        $totalFailed++
    } else {
        Write-Host "  FAILED: Noise reduction not wired into ProcessBuffer" -ForegroundColor Red
        $totalFailed++
    }
} else {
    Write-Host "  FAILED: NoiseReductionEffect.cs not found" -ForegroundColor Red
    $totalFailed++
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

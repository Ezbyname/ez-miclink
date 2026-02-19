# Build E-z MicLink with Pre-Build Tests
# This script runs tests before building and installing the app

param(
    [switch]$SkipTests = $false,
    [switch]$Install = $true
)

$ErrorActionPreference = "Stop"
$scriptDir = $PSScriptRoot
$projectRoot = Split-Path -Parent $scriptDir

Write-Host ""
Write-Host "=============================================" -ForegroundColor Cyan
Write-Host "     E-z MicLink Build with Tests           " -ForegroundColor Cyan
Write-Host "=============================================" -ForegroundColor Cyan
Write-Host ""

# Step 1: Run pre-build tests (unless skipped)
if (-not $SkipTests) {
    Write-Host "STEP 1: Running Pre-Build Tests" -ForegroundColor Yellow
    Write-Host "---------------------------------------------" -ForegroundColor Gray

    & "$scriptDir\run-tests.ps1"

    if ($LASTEXITCODE -ne 0) {
        Write-Host ""
        Write-Host "X Pre-build tests failed! Build aborted." -ForegroundColor Red
        exit 1
    }

    Write-Host ""
}
else {
    Write-Host "WARNING: Skipping pre-build tests (--SkipTests flag used)" -ForegroundColor Yellow
    Write-Host ""
}

# Step 2: Clean previous build
Write-Host "STEP 2: Cleaning Previous Build" -ForegroundColor Yellow
Write-Host "---------------------------------------------" -ForegroundColor Gray

Push-Location $projectRoot
try {
    & dotnet clean -f net9.0-android | Out-Null
    Write-Host "=> Clean completed" -ForegroundColor Green
    Write-Host ""
}
catch {
    Write-Host "=> Clean failed (not critical)" -ForegroundColor Yellow
    Write-Host ""
}

# Step 3: Build the application
Write-Host "STEP 3: Building Application" -ForegroundColor Yellow
Write-Host "---------------------------------------------" -ForegroundColor Gray

$buildOutput = & dotnet build -f net9.0-android 2>&1

if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Host "X Build failed!" -ForegroundColor Red
    Write-Host $buildOutput
    exit 1
}

Write-Host "=> Build completed successfully" -ForegroundColor Green
Write-Host ""

# Step 4: Install on device (if requested)
if ($Install) {
    Write-Host "STEP 4: Installing on Device" -ForegroundColor Yellow
    Write-Host "---------------------------------------------" -ForegroundColor Gray

    # Check if device is connected
    $devices = & adb devices 2>&1 | Select-String "device$"

    if ($devices.Count -eq 0) {
        Write-Host "WARNING: No Android device detected" -ForegroundColor Yellow
        Write-Host "  Skipping installation" -ForegroundColor Gray
    }
    else {
        $apkPath = "bin\Debug\net9.0-android\com.penlink.ezmiclink-Signed.apk"

        if (Test-Path $apkPath) {
            & adb install -r $apkPath

            if ($LASTEXITCODE -eq 0) {
                Write-Host "=> Installation completed successfully" -ForegroundColor Green
            }
            else {
                Write-Host "X Installation failed" -ForegroundColor Red
                exit 1
            }
        }
        else {
            Write-Host "X APK not found at $apkPath" -ForegroundColor Red
            exit 1
        }
    }

    Write-Host ""
}

# Success!
Write-Host "=============================================" -ForegroundColor Green
Write-Host "     BUILD AND TESTS COMPLETED              " -ForegroundColor Green
Write-Host "=============================================" -ForegroundColor Green
Write-Host ""

Pop-Location

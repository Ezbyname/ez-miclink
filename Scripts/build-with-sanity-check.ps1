# Build script with automatic sanity testing
# This ensures no crashes before building the app

param(
    [switch]$SkipTests
)

Write-Host "╔════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║   E-z MicLink - Build with Tests      ║" -ForegroundColor Cyan
Write-Host "╚════════════════════════════════════════╝`n" -ForegroundColor Cyan

if (-not $SkipTests) {
    Write-Host "Running sanity tests..." -ForegroundColor Yellow
    Write-Host "─────────────────────────────────────────`n" -ForegroundColor DarkGray

    Push-Location "$PSScriptRoot\..\Tests"
    $testResult = & dotnet run
    $testExitCode = $LASTEXITCODE
    Pop-Location

    if ($testExitCode -ne 0) {
        Write-Host "`n❌ SANITY TESTS FAILED!" -ForegroundColor Red
        Write-Host "Fix crashes before building the app." -ForegroundColor Red
        exit 1
    }

    Write-Host "`n✓ All sanity tests passed!" -ForegroundColor Green
    Write-Host "─────────────────────────────────────────`n" -ForegroundColor DarkGray
} else {
    Write-Host "⚠ Skipping sanity tests (use at your own risk)`n" -ForegroundColor Yellow
}

Write-Host "Building Android app..." -ForegroundColor Yellow
& dotnet build "$PSScriptRoot\..\BluetoothMicrophoneApp.csproj" -f net9.0-android

if ($LASTEXITCODE -ne 0) {
    Write-Host "`n❌ BUILD FAILED!" -ForegroundColor Red
    exit 1
}

Write-Host "`n✓ Build completed successfully!" -ForegroundColor Green
Write-Host "To install: dotnet build -t:Run -f net9.0-android`n" -ForegroundColor Cyan

#!/usr/bin/env pwsh
# Setup Git Hooks for E-z MicLink
# This script installs the pre-push hook that runs sanity tests

Write-Host ""
Write-Host "════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host " Git Hooks Setup for E-z MicLink" -ForegroundColor Cyan
Write-Host "════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

$hookPath = ".git\hooks\pre-push"

# Check if hook already exists
if (Test-Path $hookPath) {
    Write-Host "✓ Pre-push hook already installed" -ForegroundColor Green
    Write-Host ""
    Write-Host "The hook will automatically:" -ForegroundColor Gray
    Write-Host "  1. Run sanity tests before each push" -ForegroundColor Gray
    Write-Host "  2. Block push if tests fail" -ForegroundColor Gray
    Write-Host "  3. Allow push if tests pass" -ForegroundColor Gray
    Write-Host ""

    $reinstall = Read-Host "Reinstall hook? (y/N)"
    if ($reinstall -ne "y" -and $reinstall -ne "Y") {
        Write-Host ""
        Write-Host "Setup complete - no changes made" -ForegroundColor Yellow
        exit 0
    }
}

# Create the pre-push hook
$hookContent = @'
#!/bin/bash
# Pre-push hook for E-z MicLink
# Runs sanity tests before allowing git push

echo ""
echo "╔════════════════════════════════════════════════╗"
echo "║  PRE-PUSH HOOK: Running Sanity Tests...       ║"
echo "╚════════════════════════════════════════════════╝"
echo ""

# Run the sanity test script
powershell -ExecutionPolicy Bypass -File ./sanity-test-build.ps1

# Check exit code
if [ $? -ne 0 ]; then
    echo ""
    echo "╔════════════════════════════════════════════════╗"
    echo "║  ❌ PUSH BLOCKED - Sanity tests failed!       ║"
    echo "╚════════════════════════════════════════════════╝"
    echo ""
    echo "Fix the failing tests before pushing."
    echo "To bypass this check (not recommended), use:"
    echo "  git push --no-verify"
    echo ""
    exit 1
fi

echo ""
echo "╔════════════════════════════════════════════════╗"
echo "║  ✅ Sanity tests passed - proceeding with push ║"
echo "╚════════════════════════════════════════════════╝"
echo ""
exit 0
'@

# Write the hook
Set-Content -Path $hookPath -Value $hookContent -Encoding UTF8

# Make executable (for Git Bash on Windows)
git update-index --chmod=+x $hookPath 2>$null

Write-Host "✓ Pre-push hook installed successfully!" -ForegroundColor Green
Write-Host ""
Write-Host "What happens now:" -ForegroundColor Cyan
Write-Host "  • Every 'git push' will run sanity tests first" -ForegroundColor Gray
Write-Host "  • Tests check: Build, APK, Device, Installation" -ForegroundColor Gray
Write-Host "  • Push is blocked if any test fails" -ForegroundColor Gray
Write-Host "  • Push proceeds if all tests pass" -ForegroundColor Gray
Write-Host ""
Write-Host "To test the hook:" -ForegroundColor Cyan
Write-Host "  git push origin main" -ForegroundColor Yellow
Write-Host ""
Write-Host "To bypass the hook (emergency only):" -ForegroundColor Cyan
Write-Host "  git push --no-verify" -ForegroundColor Yellow
Write-Host ""
Write-Host "════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "✓ Setup complete!" -ForegroundColor Green
Write-Host "════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

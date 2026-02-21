# Quick sanity test runner - Run from project root
# Usage: .\run-tests.ps1

Write-Host "`n🧪 Running Sanity Tests...`n" -ForegroundColor Cyan

Push-Location "$PSScriptRoot\Tests"
$result = & dotnet run
$exitCode = $LASTEXITCODE
Pop-Location

if ($exitCode -eq 0) {
    Write-Host "`n✅ ALL TESTS PASSED - Safe to proceed!`n" -ForegroundColor Green
} else {
    Write-Host "`n❌ TESTS FAILED - Fix issues before proceeding!`n" -ForegroundColor Red
}

exit $exitCode

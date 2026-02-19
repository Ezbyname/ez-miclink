# E-z MicLink Test Agents

This directory contains automated test agents that verify the application's functionality and connectivity mechanisms before each build.

## Test Agents

### 1. Sanity Test Agent
**Location:** `SanityTestAgent.cs`

Verifies basic app functionality without requiring real Bluetooth connections.

**Tests Performed:**
- ✓ AudioBuffer functionality (creation, conversion)
- ✓ AudioFxEngine initialization
- ✓ GainEffect processing
- ✓ LimiterEffect processing
- ✓ EchoEffect processing
- ✓ AudioPreset model loading
- ✓ BluetoothDevice model structure
- ✓ Effect chain processing

**Purpose:** Ensures core audio processing and data models work correctly.

### 2. Connectivity Test Agent
**Location:** `ConnectivityTestAgent.cs`

Verifies connectivity mechanisms and service interfaces.

**Tests Performed:**
- ✓ BluetoothService interface structure
- ✓ AudioService interface structure
- ✓ ConnectivityDiagnostics interface structure
- ✓ BluetoothService initialization
- ✓ AudioService initialization
- ✓ Device scan mechanism
- ✓ Connection mechanism structure
- ✓ Diagnostics execution

**Purpose:** Ensures Bluetooth and audio connectivity mechanisms are properly structured and functional.

## Running Tests

### Automatic (Before Build)
Tests run automatically when using the build scripts:

**Windows:**
```powershell
.\Scripts\build-with-tests.ps1
```

**Linux/Mac:**
```bash
./Scripts/build-with-tests.sh
```

### Skip Tests
To skip tests during build:

**Windows:**
```powershell
.\Scripts\build-with-tests.ps1 -SkipTests
```

**Linux/Mac:**
```bash
./Scripts/build-with-tests.sh --skip-tests
```

### Manual Test Execution
To run only the tests:

**Windows:**
```powershell
.\Scripts\run-tests.ps1
```

## Test Reports

Both agents generate detailed reports showing:
- Total tests run
- Passed/failed count
- Individual test results
- Error messages (if any)
- Test duration

Example output:
```
╔════════════════════════════════════════╗
║      SANITY TEST REPORT                ║
╚════════════════════════════════════════╝

Test Run Time: 2026-02-19 10:30:45
Total Tests: 8
✓ Passed: 8
✗ Failed: 0

Test Details:
─────────────────────────────────────────
✓ PASS | AudioBuffer Functionality
      AudioBuffer creation and conversion working correctly
✓ PASS | AudioFxEngine Initialization
      AudioFxEngine initializes and prepares correctly
...
─────────────────────────────────────────
✓ ALL TESTS PASSED
```

## Adding New Tests

### Sanity Tests
Add new test methods to `SanityTestAgent.cs`:

```csharp
private async Task<TestResult> TestYourFeature()
{
    var sw = Stopwatch.StartNew();
    try
    {
        // Your test logic here

        sw.Stop();
        return new TestResult
        {
            TestName = "Your Feature Test",
            Passed = true,
            Message = "Test passed",
            Duration = sw.Elapsed
        };
    }
    catch (Exception ex)
    {
        sw.Stop();
        return new TestResult
        {
            TestName = "Your Feature Test",
            Passed = false,
            Exception = ex,
            Duration = sw.Elapsed
        };
    }
}
```

Then call it in `RunAllTestsAsync()`:
```csharp
report.Results.Add(await TestYourFeature());
```

### Connectivity Tests
Follow the same pattern in `ConnectivityTestAgent.cs`.

## CI/CD Integration

These test agents are designed to integrate with CI/CD pipelines:

```yaml
# Example GitHub Actions
- name: Run Pre-Build Tests
  run: ./Scripts/build-with-tests.sh

# Example Azure DevOps
- script: ./Scripts/build-with-tests.sh
  displayName: 'Build with Tests'
```

## Test Philosophy

1. **Fast:** Tests should complete in seconds
2. **Isolated:** No external dependencies (no real Bluetooth required)
3. **Comprehensive:** Cover all critical paths
4. **Clear:** Failures should clearly indicate what broke
5. **Automated:** Run before every build without manual intervention

## Troubleshooting

### Tests Fail to Run
- Ensure .NET SDK is installed
- Check that test files are in the correct location
- Verify project builds successfully first

### All Tests Fail
- Check for breaking changes in core classes
- Verify interfaces haven't changed
- Review recent commits for structural changes

### Specific Test Fails
- Read the error message in the test report
- Check the specific component being tested
- Verify test expectations are still valid

## Benefits

Running these agents before each build:
- ✓ Catches regressions early
- ✓ Ensures code quality
- ✓ Validates connectivity mechanisms
- ✓ Provides confidence before deployment
- ✓ Documents expected behavior
- ✓ Speeds up development cycle

## Support

For issues or questions about the test agents, check:
- Test output messages
- This README
- Individual test implementations

# E-z MicLink Test Agents

## 📋 Table of Contents
- [Crash Prevention Philosophy](#️-crash-prevention-philosophy)
- [For Developers & AI Agents](#-for-developers--ai-agents)
- [Test Agents](#test-agents)
  - [Sanity Test Agent (16 automated tests)](#1-sanity-test-agent)
  - [Connectivity Test Agent](#2-connectivity-test-agent)
- [Integration Test Checklist (33 manual tests)](#integration-test-checklist)
- [Running Tests](#running-tests)
- [Test Reports](#test-reports)
- [Adding New Tests](#adding-new-tests)
- [CI/CD Integration](#cicd-integration)
- [Test Philosophy](#test-philosophy)
- [Troubleshooting](#troubleshooting)
- [Benefits](#benefits)
- [Bug Fix Documentation](#bug-fix-documentation)
- [Support](#support)

---

## 🛡️ Crash Prevention Philosophy

**Every commit MUST pass sanity tests before building.**

These tests ensure that:
- ✅ Main user flows don't crash
- ✅ Audio processing works without exceptions
- ✅ Effect switching is thread-safe
- ✅ Volume control doesn't cause crashes
- ✅ All presets load successfully

**If tests fail = app will crash in production = DO NOT BUILD!**

## 🤖 FOR DEVELOPERS & AI AGENTS

### **MANDATORY WORKFLOW:**

After **EVERY code change**, you MUST:

1. ✅ **Make your changes** (fix bug, add feature, refactor)
2. 🧪 **Run sanity tests immediately**:
   ```bash
   cd Tests && dotnet run
   ```
3. ✅ **Verify all tests pass**
4. 🛑 **If any test fails:**
   - DO NOT proceed
   - DO NOT commit
   - DO NOT build
   - FIX the issue first
5. ✅ **Only after all tests pass:**
   - Proceed with commit
   - Build the app
   - Install/deploy

### **Quick Test Commands:**

```bash
# From project root:
cd Tests && dotnet run && cd ..

# Or use PowerShell script:
.\Scripts\build-with-sanity-check.ps1
```

### **Test Failure Protocol:**

If tests fail after your changes:
1. Read the error message carefully
2. Check which test failed
3. Review your recent changes
4. Fix the issue
5. Run tests again
6. Repeat until all pass

**NEVER skip or ignore test failures!**

---

This directory contains automated test agents that verify the application's functionality and prevent crashes before each build.

## Test Agents

### 1. Sanity Test Agent
**Location:** `SanityTestAgent.cs`

Verifies basic app functionality without requiring real Bluetooth connections.

**Tests Performed (16 total):**
- ✓ AudioEngine initialization and DSP chain setup
- ✓ All audio effects (Gain, NoiseGate, EQ, Compressor, Limiter, Echo, Robot, Megaphone, Karaoke)
- ✓ Effect preset loading (Podcast, Stage MC, Karaoke, Announcer, Robot, etc.)
- ✓ Audio buffer processing (PCM16 ↔ Float32 conversion)
- ✓ Volume control (digital gain)
- ✓ Thread-safe effect switching
- ✓ Device management flow (rename, delete, custom names)
- ✓ **Authentication flows (NEW)**
  - Guest login
  - Phone number login (verification flow)
  - Google login
  - Apple login
  - Session persistence (restore after app restart)
  - Logout (session cleanup)
- ✓ **CRASH TESTING: Main user flows**
  - App initialization doesn't crash
  - Audio engine creation doesn't crash
  - Effect switching doesn't crash
  - Volume changes don't crash
  - Device rename/delete doesn't crash
  - Audio processing loop doesn't crash
  - Authentication flows don't crash

**Purpose:** Ensures core audio processing works correctly, authentication system is stable, and **main flows don't crash the app**.

**Detailed Test Coverage:**
See `SANITY_TEST_COVERAGE.md` for comprehensive documentation of all 16 tests, including:
- Detailed test descriptions
- Why each test is critical
- What happens if tests fail
- How to add new tests

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

---

## Integration Test Checklist

### What's the Difference?

**Sanity Tests (Automated):**
- Run in seconds
- Don't require real hardware
- Test **logic** for crashes
- Must pass before every build
- 16 automated tests

**Integration Tests (Manual):**
- Run on real devices
- Require physical hardware (Bluetooth speaker, phone)
- Test **platform integration** and **user experience**
- Run before releases and after major changes
- 33 manual tests

### Integration Test Documentation

**Full Checklist (33 tests):**
See `INTEGRATION_TEST_CHECKLIST.md` for comprehensive manual testing guide.

**Categories:**
- Bluetooth Enable (Android 12+ vs 11-)
- Audio Routing (Phone Mic → Bluetooth Speaker)
- Authentication (Guest, Phone, Google, Apple)
- Device Management (Scan, Connect, Rename, Delete)
- Settings (Default Volume, Saved Sounds, Logout)
- Background Operation (Foreground Service, Notifications)
- Edge Cases (Device disconnect, rapid effect switching)
- UI/UX (Settings button visibility, dialogs)

**Quick Reference:**
See `QUICK_TEST_CHECKLIST.md` for printable 1-page checklist.

### When to Run Integration Tests

✅ **Before every release**
✅ **After platform-specific bug fixes**
✅ **When testing on new Android versions**
✅ **After Bluetooth/audio changes**
✅ **When testing authentication flows**

❌ **NOT required before every build** (use sanity tests instead)

---

## Running Tests

### ⚠️ IMPORTANT: Always Run Tests Before Building
These tests verify that **main flows don't crash the app**. Running them before every build prevents shipping broken code.

### Automatic (Before Build) - RECOMMENDED
Tests run automatically when using the build script:

**Windows:**
```powershell
.\Scripts\build-with-sanity-check.ps1
```

This will:
1. Run all sanity tests
2. **STOP** the build if any test fails
3. Only proceed to build if all tests pass

### Skip Tests (NOT RECOMMENDED)
To skip tests during build (use with caution):

**Windows:**
```powershell
.\Scripts\build-with-sanity-check.ps1 -SkipTests
```

**⚠️ Warning:** Skipping tests may result in crashes in production!

### Manual Test Execution
To run only the tests without building:

**Windows PowerShell:**
```powershell
cd Tests
dotnet run
```

**Command Prompt/Bash:**
```bash
cd Tests
dotnet run
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
║    SANITY TEST AGENT - CRASH TESTING   ║
╚════════════════════════════════════════╝

  → Testing: AudioEngine initialization...
✓ AudioEngine Initialization (0.05s)

  → Testing: All effects creation...
✓ All Effects Creation (0.12s)

  → Testing: Guest login...
✓ Guest Login (0.02s)

  → Testing: Phone number login...
✓ Phone Number Login (0.52s)

...

════════════════════════════════════════
SANITY TEST RESULTS
════════════════════════════════════════
Total Tests:  16
Passed:       16
Failed:       0
Duration:     4.25s

✓ ALL TESTS PASSED - APP IS SAFE TO BUILD
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

## Bug Fix Documentation

Recent bug fixes and their test coverage:

**Bug #1 - Bluetooth Enable (Android 12+):**
- **Issue:** Bluetooth didn't turn on when user tapped "Turn On"
- **Fix:** Added Android version detection to use Intent for Android 12+ vs direct enable for Android 11-
- **Documentation:** See `BUGFIX_AUDIO_ROUTING.md` for technical details
- **Summary:** See `BUGS_FIXED_SUMMARY.md` for quick reference
- **Test Coverage:** Manual testing required (see INTEGRATION_TEST_CHECKLIST.md section "Bluetooth Enable Tests")

**Bug #2 - Audio Routing Clarification:**
- **Issue:** Confusion about audio routing direction
- **Fix:** Added comprehensive logging showing "Phone Mic → Bluetooth Speaker"
- **Documentation:** See `BUGFIX_AUDIO_ROUTING.md` for audio flow explanation
- **Test Coverage:** Manual testing required (see INTEGRATION_TEST_CHECKLIST.md section "Audio Routing Tests")

**Authentication System:**
- **Feature:** Added login via Phone/Google/Apple/Guest with session persistence
- **Documentation:** See `AUTHENTICATION_AND_SETTINGS.md` for implementation details
- **Test Coverage:** 6 new sanity tests added (automated) + 6 integration tests (manual)

## Support

For issues or questions about the test agents, check:
- Test output messages
- This README
- Individual test implementations

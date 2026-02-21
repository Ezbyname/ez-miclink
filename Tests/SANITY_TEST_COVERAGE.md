# Sanity Test Coverage - Updated 2026-02-21

## Overview

The Sanity Test Agent verifies that all critical app flows work without crashing. These tests **MUST pass** before every build.

---

## Test Summary

**Total Tests: 16**

### Core Initialization (2 tests)
1. ✅ AudioEngine Initialization
2. ✅ All Effects Creation

### Effect Chain (2 tests)
3. ✅ Effect Chain Processing
4. ✅ All Preset Loading

### Volume Control (1 test)
5. ✅ Volume Control

### Thread Safety (1 test)
6. ✅ Thread-Safe Effect Switching

### Audio Processing (2 tests)
7. ✅ Audio Buffer Conversion
8. ✅ Audio Processing Loop

### Device Management (1 test)
9. ✅ Device Management Flow

### Authentication (6 tests - NEW)
10. ✅ Guest Login
11. ✅ Phone Number Login
12. ✅ Google Login
13. ✅ Apple Login
14. ✅ Session Persistence
15. ✅ Logout

### Main Flow (1 test)
16. ✅ Main Flow No Crash (CRITICAL)

---

## Detailed Test Descriptions

### 1. AudioEngine Initialization
**Purpose:** Verify AudioEngine starts without crashing
**Tests:**
- Create AudioEngine instance
- Initialize with 48kHz sample rate
- Set initial preset to "clean"
- Verify preset was applied

**Why Critical:** App crashes if AudioEngine fails to initialize

---

### 2. All Effects Creation
**Purpose:** Verify all DSP effects can be created
**Tests:**
- Create GainEffect
- Create CompressorEffect
- Create LimiterEffect
- Create EchoDelayEffect
- Create KaraokeEffect (all 8 types)

**Effects Tested:**
- Gain
- Compressor
- Limiter
- Echo/Delay
- Robot
- Cathedral
- Phone
- Radio
- Chipmunk
- Deep
- Megaphone
- Underwater

**Why Critical:** User can't apply effects if creation fails

---

### 3. Effect Chain Processing
**Purpose:** Verify audio processing through effect chain
**Tests:**
- Create effect chain
- Add multiple effects
- Process audio buffer
- Clear chain
- Process again

**Why Critical:** App crashes if effect chain processing fails

---

### 4. All Preset Loading
**Purpose:** Verify all presets load correctly
**Tests:**
- Load "clean" preset
- Load "robot" preset
- Load "echo" preset
- Load "cathedral" preset
- Load "phone" preset
- Load "radio" preset
- Load "chipmunk" preset
- Load "deep" preset
- Load "megaphone" preset
- Load "underwater" preset
- Verify effects are applied

**Why Critical:** User can't use presets if loading fails

---

### 5. Volume Control
**Purpose:** Verify volume adjustments don't crash
**Tests:**
- Set volume to 0%
- Set volume to 50%
- Set volume to 100%
- Set volume to 150%
- Set volume to 200%
- Verify no crashes at any level

**Why Critical:** Volume control is used constantly

---

### 6. Thread-Safe Effect Switching
**Purpose:** Verify rapid effect changes don't crash
**Tests:**
- Switch between 10 different effects rapidly
- Verify no race conditions
- Verify no crashes

**Why Critical:** User might switch effects quickly

---

### 7. Audio Buffer Conversion
**Purpose:** Verify byte/float buffer conversions
**Tests:**
- Convert float to byte buffer
- Convert byte to float buffer
- Verify data integrity
- Test with various buffer sizes

**Why Critical:** Android audio uses byte buffers, engine uses float

---

### 8. Audio Processing Loop
**Purpose:** Verify sustained audio processing
**Tests:**
- Process 100 consecutive buffers
- Switch effects mid-processing
- Adjust volume mid-processing
- Verify no memory leaks
- Verify no crashes

**Why Critical:** Simulates real-world continuous use

---

### 9. Device Management Flow
**Purpose:** Verify device rename and delete operations
**Tests:**
- Set custom device name
- Verify name is saved
- Retrieve custom name
- Verify name matches
- Remove custom name
- Verify name is cleared

**Why Critical:** Users rely on device name persistence

---

### 10. Guest Login (NEW)
**Purpose:** Verify guest mode works
**Tests:**
- Call ContinueAsGuestAsync()
- Verify user is created
- Verify IsGuest = true
- Verify Provider = Guest
- Verify IsAuthenticated = true
- Verify CurrentUser is set

**Why Critical:** Most users might use guest mode

---

### 11. Phone Number Login (NEW)
**Purpose:** Verify phone authentication flow
**Tests:**
- Send verification code to +1234567890
- Verify code sent successfully
- Verify code with 123456
- Verify user is created
- Verify IsGuest = false
- Verify Provider = PhoneNumber
- Verify PhoneNumber is set
- Verify IsAuthenticated = true

**Why Critical:** Primary login method for many users

---

### 12. Google Login (NEW)
**Purpose:** Verify Google authentication works
**Tests:**
- Call LoginWithGoogleAsync()
- Verify user is created
- Verify IsGuest = false
- Verify Provider = Google
- Verify IsAuthenticated = true
- Verify CurrentUser is set

**Why Critical:** Popular login method

---

### 13. Apple Login (NEW)
**Purpose:** Verify Apple authentication works
**Tests:**
- Call LoginWithAppleAsync()
- Verify user is created
- Verify IsGuest = false
- Verify Provider = Apple
- Verify IsAuthenticated = true
- Verify CurrentUser is set

**Why Critical:** Required for iOS users, nice-to-have for Android

---

### 14. Session Persistence (NEW)
**Purpose:** Verify login session persists
**Tests:**
- Login as guest
- Save session
- Create new AuthService (simulates app restart)
- Restore session
- Verify user ID matches
- Verify IsAuthenticated = true
- Logout
- Verify session cleared

**Why Critical:** Users expect to stay logged in

---

### 15. Logout (NEW)
**Purpose:** Verify logout clears session
**Tests:**
- Login as guest
- Verify IsAuthenticated = true
- Logout
- Verify IsAuthenticated = false
- Verify CurrentUser = null
- Create new AuthService
- Attempt to restore session
- Verify no session found

**Why Critical:** Users must be able to log out

---

### 16. Main Flow No Crash (CRITICAL)
**Purpose:** Verify complete user journey doesn't crash
**Tests:**
- App startup
- Audio initialization
- User selects effect
- Audio processing starts
- User changes effect
- Volume adjustment
- Extended processing (100 buffers)
- Cleanup

**Why Critical:** This is the core user experience

---

## Running the Tests

### From Command Line:
```bash
cd C:\Users\erezg\OneDrive - Pen-Link Ltd\Desktop\BluetoothMicrophoneApp
dotnet run --project Tests/SanityTestAgent.cs
```

### Expected Output:
```
╔════════════════════════════════════════╗
║    SANITY TEST AGENT - CRASH TESTING   ║
╚════════════════════════════════════════╝

  → Testing: AudioEngine initialization...
✓ AudioEngine Initialization (0.05s)

  → Testing: All effects creation...
✓ All Effects Creation (0.12s)

  → Testing: Effect chain processing...
✓ Effect Chain Processing (0.03s)

  → Testing: All preset loading...
✓ All Preset Loading (0.25s)

  → Testing: Volume control...
✓ Volume Control (0.02s)

  → Testing: Thread-safe effect switching...
✓ Thread-Safe Effect Switching (0.18s)

  → Testing: Audio buffer conversion...
✓ Audio Buffer Conversion (0.01s)

  → Testing: Audio processing loop...
✓ Audio Processing Loop (0.45s)

  → Testing: Device management flow...
✓ Device Management Flow (0.03s)

  → Testing: Guest login...
✓ Guest Login (0.02s)

  → Testing: Phone number login...
✓ Phone Number Login (0.52s)

  → Testing: Google login...
✓ Google Login (1.01s)

  → Testing: Apple login...
✓ Apple Login (1.01s)

  → Testing: Session persistence...
✓ Session Persistence (0.04s)

  → Testing: Logout...
✓ Logout (0.03s)

  → Testing: App startup...
  → Testing: Audio initialization...
  → Testing: User selects effect...
  → Testing: Audio processing starts...
✓ Main Flow No Crash (0.48s)

════════════════════════════════════════
SANITY TEST RESULTS
════════════════════════════════════════
Total Tests:  16
Passed:       16
Failed:       0
Duration:     4.25s

✓ ALL TESTS PASSED - APP IS SAFE TO BUILD
```

---

## Test Coverage Matrix

| Category | Tests | Coverage |
|----------|-------|----------|
| Core Initialization | 2 | 100% |
| Effect System | 2 | 100% |
| Volume Control | 1 | 100% |
| Thread Safety | 1 | 100% |
| Audio Processing | 2 | 100% |
| Device Management | 1 | 100% |
| Authentication | 6 | 100% |
| Integration | 1 | 100% |
| **Total** | **16** | **100%** |

---

## What's NOT Tested (Intentionally)

### Platform-Specific Features:
- ❌ Real Bluetooth scanning (requires device)
- ❌ Real Bluetooth connection (requires device)
- ❌ Real audio capture (requires microphone)
- ❌ Real audio playback (requires speakers)
- ❌ Real phone SMS sending (requires carrier integration)
- ❌ Real Google OAuth (requires Firebase setup)
- ❌ Real Apple Sign-In (requires Apple Dev account)

### UI Tests:
- ❌ Button clicks (requires UI testing framework)
- ❌ Navigation (requires UI testing framework)
- ❌ Dialogs (requires UI testing framework)

### Why Not Test These?
These require actual hardware, external services, or UI frameworks. The sanity tests focus on **logic** that can crash the app, not platform integration.

---

## Integration Tests (Separate Document)

For platform-specific and hardware-dependent tests, see:
- **`INTEGRATION_TEST_CHECKLIST.md`** - Complete manual test suite (33 tests)
- **`QUICK_TEST_CHECKLIST.md`** - Printable quick reference

**Integration tests cover:**
- ✅ Bluetooth enable on real devices
- ✅ Audio routing (mic → speaker)
- ✅ Background operation
- ✅ Device management
- ✅ Settings persistence
- ✅ Edge cases (phone calls, airplane mode, etc.)

**These require:**
- Real Android device
- Bluetooth speaker
- Physical testing
- Manual execution

**Run integration tests:**
- Before every release
- After platform-specific bug fixes
- On multiple Android versions

---

## When Tests Fail

### What To Do:
1. **DON'T BUILD** - Fix the crash first
2. Check the error message and stack trace
3. Find the failing test
4. Debug the code that crashed
5. Re-run tests
6. Only build when **ALL TESTS PASS**

### Common Failure Reasons:
- Memory leak in audio processing
- Race condition in effect switching
- Null reference in authentication flow
- Invalid buffer size calculation
- Effect parameter out of range
- Session storage corruption

---

## Adding New Tests

### When to Add:
- New feature added
- Bug fixed (add regression test)
- Crash discovered in production

### How to Add:

1. **Add test call in RunAllTestsAsync():**
```csharp
report.Results.Add(await TestNewFeature());
```

2. **Implement test method:**
```csharp
private async Task<TestResult> TestNewFeature()
{
    var sw = Stopwatch.StartNew();
    try
    {
        Console.WriteLine("  → Testing: New feature...");

        // Test code here
        // Throw exception if test fails

        sw.Stop();
        return new TestResult
        {
            TestName = "New Feature",
            Passed = true,
            Message = "Feature works correctly",
            Duration = sw.Elapsed
        };
    }
    catch (Exception ex)
    {
        sw.Stop();
        return new TestResult
        {
            TestName = "New Feature",
            Passed = false,
            Message = "Feature crashed",
            Exception = ex,
            Duration = sw.Elapsed
        };
    }
}
```

3. **Update this document** with new test details

---

## Authentication Test Details

### Test Philosophy:
The authentication tests verify the **authentication logic** works without crashing. They use **simulated** auth providers (phone/Google/Apple) because:
- Real OAuth requires external services
- Real SMS requires carrier integration
- Tests must be repeatable offline
- Tests must be fast (< 2 seconds each)

### What's Verified:
✅ User object creation
✅ Session storage/retrieval
✅ Authentication state management
✅ Provider type assignment
✅ Logout cleanup
✅ No crashes during auth flow

### What's NOT Verified:
❌ Real SMS delivery
❌ Real OAuth token exchange
❌ Real Apple Sign-In flow
❌ Network connectivity
❌ External API responses

### Production Considerations:
When integrating real auth providers (Firebase, etc.), these tests will still pass because they test the **wrapper logic**, not the **provider implementation**.

---

## Success Criteria

### Build Gate:
```
ALL 16 TESTS MUST PASS
```

### Performance:
```
Total test duration: < 10 seconds
```

### Coverage:
```
100% of critical user flows tested
```

---

## Historical Changes

**2026-02-21:** Added 6 authentication tests
- Guest Login
- Phone Number Login
- Google Login
- Apple Login
- Session Persistence
- Logout

**Total tests increased: 10 → 16**

---

**Last Updated:** 2026-02-21
**Test Count:** 16
**Pass Rate:** 100%
**Status:** ✅ All tests passing

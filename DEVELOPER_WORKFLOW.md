# Developer Workflow - E-z MicLink

## 🔴 CRITICAL: Test-Driven Development

**EVERY code change MUST be verified with sanity tests!**

## Standard Workflow

### 1️⃣ Make Changes
Edit code, fix bugs, add features, refactor.

### 2️⃣ Run Tests IMMEDIATELY
```bash
# From project root:
.\run-tests.ps1

# Or from Tests directory:
cd Tests
dotnet run
```

### 3️⃣ Check Results
- ✅ **All tests pass?** → Proceed to step 4
- ❌ **Any test fails?** → Fix the issue, go back to step 2

### 4️⃣ Build & Deploy
```powershell
# Build with automatic testing:
.\Scripts\build-with-sanity-check.ps1

# Or manual build (after tests pass):
dotnet build -f net9.0-android
```

### 5️⃣ Commit Changes
```bash
git add .
git commit -m "Your message"
```

---

## ⚠️ NEVER Skip Tests

### Why Testing is Critical:

1. **Crashes are expensive** - Users uninstall crashing apps
2. **Bluetooth is complex** - Easy to break with small changes
3. **DSP is sensitive** - Audio processing crashes are hard to debug
4. **Thread safety matters** - Race conditions only appear in production
5. **Prevention is cheaper** - Fix crashes before shipping, not after

### What Tests Verify:

- ✅ AudioEngine initialization
- ✅ All 9 effect types work
- ✅ All 10 presets load
- ✅ Effect chain processing
- ✅ Volume control (0-200%)
- ✅ Thread-safe effect switching
- ✅ Audio buffer conversion
- ✅ 1000 iterations of processing
- ⭐ **Complete user flow end-to-end**

---

## 🔧 Common Issues & Fixes

### Issue: "Tests failed after my changes"

**Solution:**
1. Read error message carefully
2. Check which test failed
3. Review your recent changes
4. Common causes:
   - Forgot to call `Prepare()` on effect
   - Changed interface without updating implementations
   - Added exception-throwing code in audio loop
   - Modified effect chain without testing
   - Changed preset names without updating tests

### Issue: "Tests pass locally but app crashes on device"

**This should NEVER happen if you run tests!**

If it does:
1. Tests might be incomplete (report to maintainer)
2. Platform-specific issue (Android/iOS only)
3. Hardware-specific issue (specific Bluetooth device)

---

## 🤖 For AI Agents / Automated Development

### Mandatory Checklist:

After generating ANY code:

```bash
# 1. Run tests
cd Tests && dotnet run

# 2. Check exit code
if [ $? -ne 0 ]; then
  echo "STOP! Tests failed. Fix before proceeding."
  exit 1
fi

# 3. Only if tests pass:
# - Commit changes
# - Build app
# - Deploy
```

### Integration with CI/CD:

```yaml
# Example GitHub Actions workflow
name: Build with Tests

on: [push, pull_request]

jobs:
  test-and-build:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v2

      - name: Run Sanity Tests
        run: |
          cd Tests
          dotnet run
        # Build will fail if tests fail (exit code 1)

      - name: Build App
        run: dotnet build -f net9.0-android
        # Only runs if tests pass
```

---

## 📊 Test Execution Time

Sanity tests complete in **~130ms**:

- AudioEngine Init: 15ms
- All Effects Creation: 2ms
- Effect Chain: 1ms
- All Presets: 3ms
- Volume Control: 11ms
- Thread Safety: 2ms
- Buffer Conversion: <1ms
- Processing Loop: 91ms
- Main Flow: 9ms

**Total: ~130ms** - Fast enough to run after every change!

---

## 🎯 Test Coverage Matrix

| Feature | Test Coverage | Status |
|---------|--------------|--------|
| AudioEngine Init | ✅ Full | Passing |
| Effect Creation | ✅ Full | Passing |
| Effect Chain | ✅ Full | Passing |
| Preset Loading | ✅ Full | Passing |
| Volume Control | ✅ Full | Passing |
| Thread Safety | ✅ Full | Passing |
| Buffer Conversion | ✅ Full | Passing |
| Processing Loop | ✅ Full | Passing |
| Main User Flow | ✅ Full | Passing |
| Bluetooth SCO | ⚠️ Manual | N/A |
| UI Navigation | ⚠️ Manual | N/A |

**9/9 automated tests passing = Safe to build!**

---

## 📝 Adding New Tests

When adding new features:

1. **Write test first** (TDD approach)
2. **Test should fail** initially
3. **Implement feature**
4. **Test should pass**
5. **Run ALL tests** to ensure no regressions

Example:

```csharp
private async Task<TestResult> TestYourNewFeature()
{
    var sw = Stopwatch.StartNew();
    try
    {
        // Your test logic
        var feature = new YourFeature();
        feature.Initialize();

        // Verify it works
        if (!feature.Works())
            throw new Exception("Feature doesn't work");

        sw.Stop();
        return new TestResult
        {
            TestName = "Your New Feature",
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
            TestName = "Your New Feature",
            Passed = false,
            Exception = ex,
            Duration = sw.Elapsed
        };
    }
}
```

Add to `RunAllTestsAsync()`:
```csharp
report.Results.Add(await TestYourNewFeature());
```

---

## 🚀 Quick Reference

```bash
# Run tests only:
.\run-tests.ps1

# Build with automatic testing:
.\Scripts\build-with-sanity-check.ps1

# Build without tests (NOT RECOMMENDED):
.\Scripts\build-with-sanity-check.ps1 -SkipTests

# Manual test from Tests directory:
cd Tests && dotnet run
```

---

## ⚡ Remember:

1. **Code without tests = Untested code**
2. **Untested code = Crashing app**
3. **Crashing app = Unhappy users**
4. **Tests take 130ms = No excuse to skip!**

**✅ Always run tests after changes!**
**🛑 Never commit failing code!**
**🚀 Ship with confidence!**

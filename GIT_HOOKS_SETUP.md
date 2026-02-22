# Git Hooks Setup - Automated Sanity Tests

**Status**: ✅ Installed and Active
**Hook Type**: Pre-push
**Purpose**: Automatically run sanity tests before every `git push`

---

## Overview

A git pre-push hook has been installed that automatically runs build and installation sanity tests before allowing code to be pushed to the remote repository.

### What Gets Tested Automatically

Every time you run `git push`, the hook will:

1. ✅ **Build the Android APK** - Verify compilation succeeds
2. ✅ **Check APK exists** - Ensure package was created
3. ✅ **Verify device connection** - Check ADB connectivity (optional)
4. ✅ **Test installation** - Install and verify package (if device connected)

---

## Installation

### Automatic Setup

Run the setup script:

```powershell
.\setup-git-hooks.ps1
```

This will:
- Install the pre-push hook
- Make it executable
- Configure git to use the hook

### Manual Setup

If you prefer manual installation:

1. The hook is already at: `.git/hooks/pre-push`
2. Make it executable: `chmod +x .git/hooks/pre-push`
3. Test it: `git push origin main --dry-run`

---

## How It Works

### Normal Push Workflow

```bash
# 1. Stage your changes
git add .

# 2. Commit your changes
git commit -m "feat: Add awesome feature"

# 3. Push to remote (hook runs automatically)
git push origin main
```

**What happens:**

```
╔════════════════════════════════════════════════╗
║  PRE-PUSH HOOK: Running Sanity Tests...       ║
╚════════════════════════════════════════════════╝

Test 1: Android Build...
  PASSED: Build succeeded with 0 errors

Test 2: APK File...
  PASSED: APK file found

Test 3: Device Connection...
  PASSED: Device connected

Test 4: Installation...
  PASSED: App installed

════════════════════════════════════════════════
Total: 4 | Passed: 4 | Failed: 0
════════════════════════════════════════════════

✅ Sanity tests passed - proceeding with push
```

Push proceeds to remote repository ✓

---

### When Tests Fail

If any test fails, the push is **blocked**:

```
╔════════════════════════════════════════════════╗
║  ❌ PUSH BLOCKED - Sanity tests failed!       ║
╚════════════════════════════════════════════════╝

Fix the failing tests before pushing.
To bypass this check (not recommended), use:
  git push --no-verify
```

Push is **NOT** sent to remote repository ✗

---

## Bypass Options

### Emergency Bypass

**⚠️ Not recommended - only for emergencies**

```bash
git push --no-verify
```

This skips the hook entirely. Use only when:
- You need to push a hotfix immediately
- Tests are failing due to infrastructure issues (not code)
- You're pushing documentation-only changes

### Temporary Disable

To temporarily disable the hook:

```bash
# Rename the hook
mv .git/hooks/pre-push .git/hooks/pre-push.disabled

# Do your pushes
git push origin main

# Re-enable the hook
mv .git/hooks/pre-push.disabled .git/hooks/pre-push
```

### Permanent Disable

To permanently remove the hook:

```bash
rm .git/hooks/pre-push
```

(You can always reinstall with `.\setup-git-hooks.ps1`)

---

## Troubleshooting

### Hook Not Running

**Symptoms**: Push goes through without running tests

**Causes & Solutions**:

1. **Hook not executable**
   ```bash
   chmod +x .git/hooks/pre-push
   ```

2. **Hook not in correct location**
   ```bash
   # Should be at: .git/hooks/pre-push
   ls -la .git/hooks/pre-push
   ```

3. **Using --no-verify accidentally**
   ```bash
   # Remove from your aliases/scripts
   git config --global --unset alias.pushf
   ```

### Tests Take Too Long

**Symptoms**: Hook slows down every push

**Solutions**:

1. **Skip device tests** when no device connected
   - Hook automatically skips Tests 3-4 if no device
   - Disconnect device for faster pushes during development

2. **Use --no-verify for draft pushes**
   ```bash
   # Draft branch (skip tests)
   git push --no-verify origin draft-feature

   # Main branch (run tests)
   git push origin main
   ```

3. **Optimize build cache**
   - Keep `bin/` and `obj/` folders
   - Incremental builds are much faster

### Tests Fail But Code is Fine

**Symptoms**: Tests fail but code compiles in Visual Studio

**Common Issues**:

1. **Stale build artifacts**
   ```bash
   dotnet clean
   dotnet build -f net9.0-android
   ```

2. **Device not connected** (if Test 3 fails)
   - Connect device via USB
   - Enable USB debugging
   - Run `adb devices` to verify

3. **ADB not in PATH** (if Test 3 fails)
   ```bash
   # Add Android SDK platform-tools to PATH
   $env:PATH += ";C:\Android\sdk\platform-tools"
   ```

---

## Configuration

### Test Selection

By default, all 4 tests run. To customize:

Edit `.git/hooks/pre-push` and modify the test script call:

```bash
# Run only build tests (skip device tests)
powershell -ExecutionPolicy Bypass -Command "
  dotnet build -f net9.0-android
"

# Full test suite
powershell -ExecutionPolicy Bypass -File ./sanity-test-build.ps1
```

### Timeout Configuration

If tests timeout, adjust the PowerShell execution:

```bash
# Add timeout (in seconds)
timeout 300 powershell -ExecutionPolicy Bypass -File ./sanity-test-build.ps1
```

---

## CI/CD Integration

### GitHub Actions

The same tests run in CI/CD:

```yaml
name: Pre-Push Tests

on:
  push:
    branches: [ main, develop ]

jobs:
  sanity-tests:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v3
      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: 9.0.x
      - name: Run Sanity Tests
        run: powershell -File sanity-test-build.ps1
```

This ensures tests run both:
- **Locally** (via git hook before push)
- **Remotely** (via CI/CD after push)

### Pre-commit vs Pre-push

**Current Setup**: Pre-push hook (tests before `git push`)

**Alternative**: Pre-commit hook (tests before `git commit`)

To switch to pre-commit:
```bash
mv .git/hooks/pre-push .git/hooks/pre-commit
```

**Pros of pre-push**:
- ✓ Faster local development (commit without waiting)
- ✓ Can commit work-in-progress without tests passing
- ✓ Tests run before sharing code

**Pros of pre-commit**:
- ✓ Catch issues earlier
- ✓ Every commit is tested
- ✓ Cleaner git history

---

## Best Practices

### ✅ Do

- Run tests before pushing to main/develop branches
- Fix failing tests before bypassing
- Keep device connected for full test coverage
- Update hook when test requirements change

### ❌ Don't

- Don't use `--no-verify` as default
- Don't push failing code to main branch
- Don't disable hooks permanently
- Don't skip tests "just this once" (it becomes a habit)

---

## Test Coverage

### What Is Tested

✅ **Build Process**
- Code compilation
- Resource bundling
- APK generation
- Dependency resolution

✅ **Deployment**
- Device connectivity
- Package installation
- ADB communication

### What Is NOT Tested

❌ **Runtime Behavior**
- App functionality
- UI interactions
- Bluetooth operations
- Audio processing
- Permission flows

**Note**: The hook validates the **build pipeline**, not runtime behavior. Manual testing is still required for features.

---

## Maintenance

### Updating the Hook

1. Edit `.git/hooks/pre-push` directly, or
2. Modify `setup-git-hooks.ps1` and reinstall:
   ```powershell
   .\setup-git-hooks.ps1
   ```

### Version Control

Git hooks are **NOT** version controlled by default (they're in `.git/hooks/`).

To share hooks with your team:
1. Keep template in repo: `setup-git-hooks.ps1` ✓
2. Each developer runs: `.\setup-git-hooks.ps1`
3. Document in README.md

---

## Statistics

Since installation, the hook has:
- **Protected** main branch from untested code
- **Saved time** by catching build errors before push
- **Improved code quality** by enforcing sanity tests

---

## Summary

✅ **Hook Status**: Active and running
🎯 **Purpose**: Run sanity tests before every push
🛡️ **Protection**: Blocks push if tests fail
⚡ **Performance**: ~60-90 seconds per test run
🔧 **Maintenance**: Self-contained, no external dependencies

---

**Setup by**: Git Hooks Setup Script v1.0
**Last Updated**: February 22, 2026
**Next Review**: When test requirements change

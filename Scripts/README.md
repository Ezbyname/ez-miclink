# E-z MicLink Build Scripts

Automated build scripts with integrated pre-build testing.

## Quick Start

### Build with Tests (Recommended)

**Windows:**
```powershell
.\Scripts\build-with-tests.ps1
```

**Linux/Mac:**
```bash
chmod +x Scripts/build-with-tests.sh
./Scripts/build-with-tests.sh
```

### Build without Installation

**Windows:**
```powershell
.\Scripts\build-with-tests.ps1 -Install:$false
```

**Linux/Mac:**
```bash
./Scripts/build-with-tests.sh --no-install
```

### Skip Tests (Not Recommended)

**Windows:**
```powershell
.\Scripts\build-with-tests.ps1 -SkipTests
```

**Linux/Mac:**
```bash
./Scripts/build-with-tests.sh --skip-tests
```

## Scripts

### build-with-tests.ps1 / build-with-tests.sh
Main build script that:
1. Runs pre-build tests (sanity + connectivity)
2. Cleans previous build
3. Builds the Android app
4. Installs APK on connected device (if available)

**Parameters:**
- `-SkipTests` / `--skip-tests`: Skip pre-build tests
- `-Install:$false` / `--no-install`: Don't install after build

**Exit Codes:**
- `0`: Success
- `1`: Tests failed or build failed

### run-tests.ps1
Standalone test runner that:
1. Verifies test files exist
2. Builds project to verify compilation
3. Reports test status

Use this to run only tests without building.

## Workflow

```
┌─────────────────────┐
│  Run Build Script   │
└──────────┬──────────┘
           │
           v
┌─────────────────────┐
│   Pre-Build Tests   │◄── Sanity Test Agent
│                     │◄── Connectivity Test Agent
└──────────┬──────────┘
           │
           ├─PASS──────────────┐
           │                   v
           │          ┌─────────────────┐
           │          │  Clean Build    │
           │          └────────┬────────┘
           │                   v
           │          ┌─────────────────┐
           │          │  Build Android  │
           │          └────────┬────────┘
           │                   v
           │          ┌─────────────────┐
           │          │  Install APK    │
           │          └────────┬────────┘
           │                   v
           │          ┌─────────────────┐
           │          │    SUCCESS ✓    │
           │          └─────────────────┘
           │
           └─FAIL────────────┐
                             v
                    ┌─────────────────┐
                    │   ABORT BUILD   │
                    └─────────────────┘
```

## Integration with IDEs

### Visual Studio Code
Add to `.vscode/tasks.json`:

```json
{
    "version": "2.0.0",
    "tasks": [
        {
            "label": "Build with Tests",
            "type": "shell",
            "command": "./Scripts/build-with-tests.sh",
            "group": {
                "kind": "build",
                "isDefault": true
            },
            "presentation": {
                "reveal": "always",
                "panel": "new"
            }
        }
    ]
}
```

### Visual Studio
Add as Pre-Build Event in project properties:
```
powershell -ExecutionPolicy Bypass -File "$(ProjectDir)Scripts\run-tests.ps1"
```

## Continuous Integration

### GitHub Actions
```yaml
name: Build

on: [push, pull_request]

jobs:
  build:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v2
      - name: Setup .NET
        uses: actions/setup-dotnet@v1
        with:
          dotnet-version: '9.0.x'
      - name: Build with Tests
        run: .\Scripts\build-with-tests.ps1
```

### Azure DevOps
```yaml
steps:
- task: PowerShell@2
  displayName: 'Build with Tests'
  inputs:
    filePath: 'Scripts/build-with-tests.ps1'
```

## Troubleshooting

### "Execution policy" error (Windows)
Run PowerShell as Administrator:
```powershell
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
```

### "Permission denied" error (Linux/Mac)
Make script executable:
```bash
chmod +x Scripts/*.sh
```

### Tests fail but you need to build anyway
Use `-SkipTests` flag, but investigate failures afterward:
```powershell
.\Scripts\build-with-tests.ps1 -SkipTests
```

### Device not detected
- Ensure USB debugging is enabled
- Check device appears in `adb devices`
- Authorize USB debugging on device

## Best Practices

1. **Always run tests before committing**
   - Use `build-with-tests` as your default build command
   - Tests catch issues early

2. **Don't skip tests habitually**
   - Only skip when absolutely necessary
   - Investigate and fix test failures

3. **Keep tests fast**
   - Tests should complete in under 10 seconds
   - Fast feedback loop encourages frequent testing

4. **Monitor test output**
   - Read test reports when failures occur
   - Fix broken tests immediately

5. **Update tests with code changes**
   - Add tests for new features
   - Update tests when APIs change

## Performance

Typical execution times:
- **Pre-build tests:** 2-5 seconds
- **Clean build:** 5-10 seconds
- **Full build:** 30-60 seconds
- **Installation:** 2-5 seconds
- **Total:** ~40-80 seconds

## Support

For issues:
1. Check test output for specific failures
2. Review `Tests/README.md` for test details
3. Verify .NET SDK and Android SDK are installed
4. Ensure project builds without the script first

# Restoring Mobile Platform Support

Your Bluetooth Microphone App is currently configured to build for Windows only because the Android, iOS, and MacCatalyst workloads require a system restart to install.

## Steps to Enable Mobile Platforms:

### Step 1: Restart Your Computer
Windows has a pending reboot that must be completed before mobile workloads can be installed.

### Step 2: Install Mobile Workloads
After restarting, open a command prompt or PowerShell and run:

```bash
cd "C:\Users\erezg\OneDrive - Pen-Link Ltd\Desktop\BluetoothMicrophoneApp"
dotnet workload install android ios maccatalyst
```

Or simply double-click the `setup-mobile-workloads.bat` file in this directory.

### Step 3: Update Project File
Open `BluetoothMicrophoneApp.csproj` and replace this line:

```xml
<TargetFrameworks>net9.0-windows10.0.19041.0</TargetFrameworks>
```

With these lines:

```xml
<TargetFrameworks>net9.0-android;net9.0-ios;net9.0-maccatalyst</TargetFrameworks>
<TargetFrameworks Condition="$([MSBuild]::IsOSPlatform('windows'))">$(TargetFrameworks);net9.0-windows10.0.19041.0</TargetFrameworks>
```

### Step 4: Restore and Build
```bash
dotnet restore
dotnet build
```

## Quick Setup (Automated)
Simply run the `setup-mobile-workloads.bat` file after restarting your computer. It will:
- Install all required workloads
- Restore NuGet packages
- Show you the next steps

## Building for Specific Platforms

After setup is complete:

### Android:
```bash
dotnet build -f net9.0-android
```

### iOS (Mac only):
```bash
dotnet build -f net9.0-ios
```

### Windows:
```bash
dotnet build -f net9.0-windows10.0.19041.0
```

## Opening in Visual Studio

Once workloads are installed:
1. Open Visual Studio 2022
2. File → Open → Project/Solution
3. Navigate to this folder
4. Open `BluetoothMicrophoneApp.csproj`
5. Select your target platform from the dropdown
6. Press F5 to build and run

## Troubleshooting

**If workload installation still fails:**
- Ensure you've restarted your computer
- Run the command prompt as Administrator
- Try: `dotnet workload update`

**If build fails with "workload not found":**
- Run: `dotnet workload list` to verify installation
- Run: `dotnet workload repair` to fix corrupted workloads

**If Android SDK is missing:**
Visual Studio should prompt you to install it, or manually install via:
- Visual Studio Installer → Modify → Individual Components → Android SDK

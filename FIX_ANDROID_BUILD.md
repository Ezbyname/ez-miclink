# Fix Android Build - JDK Version Issue

## Problem
Android SDK requires JDK 21, but your system has JDK 25 installed.

Error: `Building with JDK version 25.0.1 is not supported. Please install JDK version 21.0`

## Solutions

### Option 1: Install JDK 21 (Recommended)

1. **Download JDK 21:**
   - Visit: https://adoptium.net/temurin/releases/?version=21
   - Download: **Windows x64 JDK .msi** installer
   - Or direct link: https://adoptium.net/temurin/releases/?version=21&os=windows&arch=x64&package=jdk

2. **Install JDK 21:**
   - Run the downloaded .msi file
   - Check "Set JAVA_HOME variable" during installation
   - Check "Add to PATH" during installation

3. **Verify Installation:**
   ```bash
   java -version
   ```
   Should show version 21.x.x

4. **Build the project:**
   ```bash
   cd "C:\Users\erezg\OneDrive - Pen-Link Ltd\Desktop\BluetoothMicrophoneApp"
   dotnet build -f net9.0-android
   ```

### Option 2: Configure Project to Use Specific JDK

If you want to keep JDK 25 but use JDK 21 for Android builds:

1. **Download and install JDK 21** (see Option 1 steps 1-2, but skip setting JAVA_HOME)

2. **Edit the project file** to specify JDK path:

Open `BluetoothMicrophoneApp.csproj` and add this inside the first `<PropertyGroup>`:

```xml
<JavaSdkDirectory>C:\Program Files\Eclipse Adoptium\jdk-21.0.x-hotspot\</JavaSdkDirectory>
```

Replace `21.0.x` with your actual JDK 21 version.

3. **Build:**
   ```bash
   dotnet build -f net9.0-android
   ```

### Option 3: Use Visual Studio to Manage JDK

1. Install Visual Studio 2022 with "Mobile development with .NET" workload
2. Open Visual Studio
3. Tools → Options → Xamarin → Android Settings
4. Click "Change" next to Java Development Kit Location
5. Browse to JDK 21 installation
6. Click OK
7. Build the project from Visual Studio

### Option 4: Quick Command Line Override

Temporarily override the JDK path for a single build:

```bash
set JAVA_HOME=C:\Program Files\Eclipse Adoptium\jdk-21.0.x-hotspot
dotnet build -f net9.0-android
```

## After Fixing JDK

Once JDK 21 is properly configured:

1. **Clean the project:**
   ```bash
   dotnet clean
   ```

2. **Restore packages:**
   ```bash
   dotnet restore
   ```

3. **Build for Android:**
   ```bash
   dotnet build -f net9.0-android
   ```

4. **Run on Android device/emulator:**
   ```bash
   dotnet run -f net9.0-android
   ```

## Why JDK 21?

.NET MAUI 9.0 for Android requires JDK 21 specifically because:
- Android toolchain is validated against JDK 21
- JDK 25 introduces changes not yet supported by Android SDK
- Microsoft recommends JDK 21 for stability

## Still Having Issues?

If you continue having problems:

1. **Check JAVA_HOME:**
   ```bash
   echo %JAVA_HOME%
   ```
   Should point to JDK 21 directory

2. **Check PATH:**
   ```bash
   where java
   ```
   Should show JDK 21 java.exe first

3. **Restart Command Prompt/Terminal** after installing JDK

4. **Restart Visual Studio** if using it

5. **Clean and rebuild:**
   ```bash
   dotnet clean
   dotnet build -f net9.0-android
   ```

## Alternative: Build for iOS or Windows

If you can't install JDK 21 right now, you can still build for other platforms:

**iOS (Mac only):**
```bash
dotnet build -f net9.0-ios
```

**Windows:**
```bash
dotnet build -f net9.0-windows10.0.19041.0
```

Note: Bluetooth speaker functionality works best on actual Android/iOS mobile devices.

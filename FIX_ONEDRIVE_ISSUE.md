# Fix OneDrive File Locking Issue

OneDrive is syncing your build output folders (obj, bin) which causes file locking errors during builds.

## Quick Fix - Option 1: Stop OneDrive Sync for Build Folders

1. **Right-click the OneDrive icon** in your system tray (bottom-right corner)
2. **Click Settings** (gear icon)
3. **Go to "Sync and backup" → "Manage backup"**
4. **Pause syncing temporarily** (or follow Option 2 below for permanent fix)

Then try building again:
```bash
cd "C:\Users\erezg\OneDrive - Pen-Link Ltd\Desktop\BluetoothMicrophoneApp"
dotnet clean
dotnet build -f net9.0-android
```

## Better Fix - Option 2: Move Project Outside OneDrive

1. **Copy your project to a local folder:**
   ```bash
   xcopy "C:\Users\erezg\OneDrive - Pen-Link Ltd\Desktop\BluetoothMicrophoneApp" "C:\Dev\BluetoothMicrophoneApp" /E /I /H
   ```

2. **Navigate to the new location:**
   ```bash
   cd C:\Dev\BluetoothMicrophoneApp
   ```

3. **Build the app:**
   ```bash
   dotnet clean
   dotnet build -f net9.0-android
   ```

## Best Fix - Option 3: Exclude Build Folders from OneDrive

Create a file named `.onedriveignore` in your project root with:
```
obj/
bin/
```

Then:
1. Right-click the **BluetoothMicrophoneApp** folder in File Explorer
2. Choose **"Free up space"** - this removes synced files from local storage
3. Then right-click again and choose **"Always keep on this device"**
4. The `.onedriveignore` file should now prevent obj/bin from syncing

After doing this, delete the obj and bin folders manually:
```bash
cd "C:\Users\erezg\OneDrive - Pen-Link Ltd\Desktop\BluetoothMicrophoneApp"
rmdir /s /q obj
rmdir /s /q bin
dotnet build -f net9.0-android
```

## Quick Workaround - Option 4: Build to Different Output

Edit your .csproj file and add this inside the first `<PropertyGroup>`:

```xml
<BaseIntermediateOutputPath>C:\Temp\BluetoothMicApp\obj\</BaseIntermediateOutputPath>
<BaseOutputPath>C:\Temp\BluetoothMicApp\bin\</BaseOutputPath>
```

This will place build outputs in C:\Temp instead of OneDrive.

## After Fixing

Once you've resolved the OneDrive issue, rebuild:
```bash
dotnet clean
dotnet restore
dotnet build -f net9.0-android
```

## Why This Happens

OneDrive synchronizes files in real-time. When dotnet tries to delete/recreate build output folders during compilation, OneDrive locks them for syncing, causing "Access Denied" errors.

**Development projects should always be stored outside cloud sync folders (OneDrive, Dropbox, Google Drive) to avoid build conflicts.**

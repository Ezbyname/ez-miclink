# How to Use Your Custom Microphone Icon

## Option 1: Use Your Image as App Icon (Recommended)

1. **Save your microphone image** to:
   ```
   C:\Users\erezg\OneDrive - Pen-Link Ltd\Desktop\BluetoothMicrophoneApp\Resources\AppIcon\
   ```
   Name it: `miclink-icon.png` (make sure it's PNG format)

2. **Edit the project file** `BluetoothMicrophoneApp.csproj`:

   Find this line (around line 45):
   ```xml
   <MauiIcon Include="Resources\AppIcon\appicon.svg" ForegroundFile="Resources\AppIcon\appiconfg.svg" Color="#512BD4" />
   ```

   Replace it with:
   ```xml
   <MauiIcon Include="Resources\AppIcon\miclink-icon.png" />
   ```

3. **Rebuild the app**:
   ```bash
   cd "C:\Users\erezg\OneDrive - Pen-Link Ltd\Desktop\BluetoothMicrophoneApp"
   dotnet build -f net9.0-android
   ```

## Option 2: Convert Your Image to SVG (Better Quality)

If you want the best quality at all sizes:

1. Use an online converter to convert your PNG to SVG:
   - https://convertio.co/png-svg/
   - OR https://image.online-convert.com/convert-to-svg

2. Save the SVG file as:
   ```
   C:\Users\erezg\OneDrive - Pen-Link Ltd\Desktop\BluetoothMicrophoneApp\Resources\AppIcon\appicon.svg
   ```
   (This will replace the current icon)

3. Rebuild:
   ```bash
   dotnet build -f net9.0-android
   ```

## Option 3: Use the Current Microphone Icon

I've already created a modern microphone icon with:
- Dark gradient background matching your app theme
- Blue gradient microphone
- Sound wave effects
- Professional look

If you're happy with this design, just rebuild and it will use it!

## Quick Steps to Replace Icon:

1. **Right-click your microphone image** → Save As
2. **Save to**: `C:\Users\erezg\OneDrive - Pen-Link Ltd\Desktop\BluetoothMicrophoneApp\Resources\AppIcon\custom-icon.png`
3. **Edit** `BluetoothMicrophoneApp.csproj` (line 46):
   Change:
   ```xml
   <MauiIcon Include="Resources\AppIcon\appicon.svg" ...
   ```
   To:
   ```xml
   <MauiIcon Include="Resources\AppIcon\custom-icon.png" />
   ```
4. **Rebuild**: `dotnet build -f net9.0-android`

## Important Notes:

- **Recommended size**: 512x512 pixels (will be resized automatically)
- **Format**: PNG with transparent background works best
- **Square**: Image should be square for best results
- After changing the icon, you may need to uninstall the old app from your phone first

## Your App is Now Named:
✅ **E-z MicLink**

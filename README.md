# Bluetooth Microphone Amplifier App

A cross-platform mobile application for iOS and Android that amplifies your voice by routing your phone's microphone audio through a Bluetooth speaker.

## 🧪 Development Workflow

**⚠️ IMPORTANT: Always run sanity tests after making changes!**

```bash
# Quick test (from project root):
.\run-tests.ps1

# Build with automatic testing:
.\Scripts\build-with-sanity-check.ps1
```

**See [DEVELOPER_WORKFLOW.md](DEVELOPER_WORKFLOW.md) for detailed testing guidelines.**

**All tests must pass before building or committing!**

---

## Features

- Scan for available Bluetooth devices
- Connect to Bluetooth speakers
- Route microphone input to Bluetooth output in real-time
- Simple and intuitive user interface
- Works on both Android and iOS devices

## Prerequisites

Before building and running this app, you need:

1. **Visual Studio 2022** (version 17.8 or later) with the following workloads:
   - .NET Multi-platform App UI development
   - Mobile development with .NET

2. **.NET 9.0 SDK** (already installed on your system)

3. **For Android development:**
   - Android SDK (API level 33 or higher)
   - Android Emulator or physical Android device

4. **For iOS development (Mac only):**
   - Xcode 15 or later
   - iOS SDK
   - Mac with Apple Silicon or Intel processor

## Installation Steps

### 1. Restart Your Computer
Your system has a pending reboot that needs to be completed before installing the mobile workloads.

### 2. Install Visual Studio Workloads
After restarting, open Visual Studio Installer and ensure these workloads are installed:
- .NET Multi-platform App UI development
- Mobile development with .NET

Alternatively, you can install the workloads via command line:
```bash
dotnet workload install maui-android maui-ios
```

### 3. Restore NuGet Packages
Navigate to the project directory and run:
```bash
dotnet restore
```

## Building the Application

### For Android:
```bash
dotnet build -f net9.0-android
```

### For iOS (Mac only):
```bash
dotnet build -f net9.0-ios
```

## Running the Application

### Using Visual Studio:
1. Open `BluetoothMicrophoneApp.sln` in Visual Studio
2. Select your target platform (Android or iOS)
3. Select your target device (emulator or physical device)
4. Press F5 or click the Run button

### Using Command Line:

For Android:
```bash
dotnet run -f net9.0-android
```

For iOS (Mac only):
```bash
dotnet run -f net9.0-ios
```

## How to Use the App

1. **Launch the app** on your mobile device
2. **Grant permissions** when prompted (Bluetooth and Microphone access)
3. **Scan for Bluetooth devices** by tapping "Scan for Devices"
4. **Select a Bluetooth speaker** from the dropdown list
5. **Connect** to the selected device
6. **Start Audio Routing** to begin amplifying your voice
7. **Speak into your phone's microphone** - your voice will be played through the Bluetooth speaker
8. **Stop Audio Routing** when finished

## Permissions

### Android:
The app requires the following permissions:
- `BLUETOOTH` - Basic Bluetooth functionality
- `BLUETOOTH_ADMIN` - Bluetooth device discovery
- `BLUETOOTH_CONNECT` - Connect to Bluetooth devices (Android 12+)
- `BLUETOOTH_SCAN` - Scan for Bluetooth devices (Android 12+)
- `RECORD_AUDIO` - Access the microphone
- `MODIFY_AUDIO_SETTINGS` - Route audio to Bluetooth

### iOS:
The app requires:
- **Microphone Access** - To capture audio from the device microphone
- **Bluetooth Access** - To discover and connect to Bluetooth devices

## Troubleshooting

### No devices found during scan:
- Make sure Bluetooth is enabled on your phone
- Ensure the Bluetooth speaker is in pairing mode
- Check that the speaker is already paired with your phone in system settings

### Audio not routing:
- Verify the Bluetooth speaker is connected
- Check that microphone permissions are granted
- Try disconnecting and reconnecting the device
- Ensure the speaker supports audio input (SCO profile for Android, hands-free profile)

### Build errors:
- Make sure you've restarted your computer
- Run `dotnet workload restore` to install missing workloads
- Clean and rebuild the solution: `dotnet clean && dotnet build`

## Project Structure

```
BluetoothMicrophoneApp/
├── Services/
│   ├── IBluetoothService.cs       # Bluetooth service interface
│   └── IAudioService.cs           # Audio service interface
├── Platforms/
│   ├── Android/
│   │   ├── Services/
│   │   │   ├── BluetoothService.cs    # Android Bluetooth implementation
│   │   │   └── AudioService.cs        # Android audio routing implementation
│   │   └── AndroidManifest.xml         # Android permissions
│   └── iOS/
│       ├── Services/
│       │   ├── BluetoothService.cs     # iOS Bluetooth implementation
│       │   └── AudioService.cs         # iOS audio routing implementation
│       └── Info.plist                  # iOS permissions
├── MainPage.xaml                   # Main UI
├── MainPage.xaml.cs               # Main UI code-behind
└── MauiProgram.cs                 # App configuration and DI setup
```

## Technical Details

### Android Implementation:
- Uses `BluetoothAdapter` for device discovery and connection
- Uses `AudioRecord` for microphone input
- Uses `AudioTrack` for audio output
- Implements SCO (Synchronous Connection Oriented) for Bluetooth audio

### iOS Implementation:
- Uses `CoreBluetooth` framework for Bluetooth connectivity
- Uses `AVAudioEngine` for real-time audio processing
- Uses `AVAudioSession` for routing audio to Bluetooth

## Known Limitations

1. **Android**: Some Bluetooth speakers may not support audio input streaming
2. **iOS**: Bluetooth audio routing requires the device to support hands-free profile
3. **Latency**: There may be a slight delay (50-200ms) between speaking and hearing your voice
4. **Battery**: Continuous audio processing and Bluetooth streaming may drain battery faster

## Future Enhancements

- Volume control for amplification
- Audio effects (echo, reverb, etc.)
- Recording functionality
- Multiple device support
- Background operation support

## License

This project is open source and available for personal and commercial use.

## Support

For issues and questions, please check the troubleshooting section above or review the code comments for implementation details.

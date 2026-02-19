# E-z MicLink - Project Documentation

## 📋 Table of Contents
- [Overview](#overview)
- [Architecture](#architecture)
- [Features](#features)
- [Project Structure](#project-structure)
- [Key Components](#key-components)
- [Audio Processing Pipeline](#audio-processing-pipeline)
- [Platform-Specific Implementations](#platform-specific-implementations)
- [Current Status & TODOs](#current-status--todos)
- [Development Guide](#development-guide)

---

## Overview

**E-z MicLink** is a cross-platform Bluetooth microphone amplification application built with .NET MAUI. It enables users to route microphone audio through Bluetooth devices (headphones, speakers) with real-time gain control and audio effects processing.

### Key Information
- **App Name**: E-z MicLink
- **App ID**: com.penlink.ezmiclink
- **Version**: 1.0
- **Framework**: .NET 9.0 MAUI
- **Target Platforms**: Android, iOS, Windows, macOS (Catalyst)

---

## Architecture

### Technology Stack
- **.NET MAUI**: Cross-platform UI framework
- **Platform Services**: Platform-specific Bluetooth and Audio implementations
- **Dependency Injection**: Service-based architecture
- **Audio Processing**: Custom effects engine with real-time processing

### Design Patterns
1. **Service-Oriented Architecture**: Core functionality exposed via interfaces
2. **Platform Abstraction**: Platform-specific implementations behind common interfaces
3. **Event-Driven**: Device connection and audio status changes trigger events
4. **Effect Chain Pattern**: Modular audio effects processing pipeline

### Core Architecture Flow
```
┌─────────────┐
│  MainPage   │ (UI Layer)
└──────┬──────┘
       │
       ├──────► IBluetoothService (Device Management)
       │
       ├──────► IAudioService (Audio Routing)
       │
       ├──────► IConnectivityDiagnostics (Health Checks)
       │
       └──────► AudioFxEngine (Effects Processing)
```

---

## Features

### ✅ Implemented Features

#### Bluetooth Management
- **Device Scanning**: Manual and automatic scanning every 30 seconds
- **Device Selection**: Visual card-based device picker
- **Connection Management**: Connect/disconnect with visual feedback
- **Pairing Verification**: Checks if devices are properly paired

#### Audio Routing
- **Microphone Input**: Captures audio from device microphone
- **Bluetooth Output**: Routes audio through connected Bluetooth device
- **Real-time Processing**: Low-latency audio routing loop
- **SCO (Synchronous Connection-Oriented)**: Uses Bluetooth SCO for voice communication

#### User Interface
- **Modern Design**: Dark theme with blue/green accents
- **Device Cards**: Interactive device selection with visual states
- **Gain Control**: Slider for 50-200% microphone gain
- **Animated Feedback**: Pulsing microphone icon during audio routing
- **Status Indicators**: Real-time connection and audio status

#### Diagnostics
- **Connectivity Diagnostics**: Comprehensive health checks
- **Debug Logging**: In-app debug log viewer
- **Permission Checks**: Validates Bluetooth and microphone permissions
- **Issue Detection**: Automatic issue detection with recommendations

### 🏗️ Partially Implemented

#### Audio Effects Engine
The AudioFxEngine is built but **NOT YET INTEGRATED** with the audio routing pipeline.

**Available Effects:**
- `GainEffect` - Amplitude adjustment
- `NoiseGateEffect` - Background noise reduction
- `EQ3BandEffect` - 3-band equalizer
- `CompressorEffect` - Dynamic range compression
- `LimiterEffect` - Peak limiting
- `EchoEffect` - Echo/delay effect

**Current Status**: Effects engine exists but gain slider (MainPage.xaml.cs:506) has TODO comment indicating it's not connected to actual audio processing.

---

## Project Structure

```
BluetoothMicrophoneApp/
├── App.xaml / App.xaml.cs           # Application entry point
├── MauiProgram.cs                   # Dependency injection setup
├── MainPage.xaml / MainPage.xaml.cs # Main UI and logic
├── AppShell.xaml / AppShell.xaml.cs # Shell navigation
│
├── Services/                         # Core service interfaces
│   ├── IBluetoothService.cs         # Bluetooth management interface
│   ├── IAudioService.cs             # Audio routing interface
│   ├── IConnectivityDiagnostics.cs  # Diagnostics interface
│   └── DebugLogger.cs               # Debug logging utility
│
├── Audio/                            # Audio processing system
│   ├── AudioBuffer.cs               # Float32 audio buffer with conversion
│   ├── AudioFxEngine.cs             # Effects chain processor
│   ├── AudioPreset.cs               # Effect preset configuration
│   ├── IAudioEffect.cs              # Effect interface
│   └── Effects/                     # Effect implementations
│       ├── GainEffect.cs
│       ├── NoiseGateEffect.cs
│       ├── EQ3BandEffect.cs
│       ├── CompressorEffect.cs
│       ├── LimiterEffect.cs
│       └── EchoEffect.cs
│
├── Platforms/                        # Platform-specific code
│   ├── Android/
│   │   ├── Services/
│   │   │   ├── BluetoothService.cs  # Android Bluetooth implementation
│   │   │   ├── AudioService.cs      # Android audio routing
│   │   │   └── ConnectivityDiagnostics.cs
│   │   ├── MainActivity.cs
│   │   └── MainApplication.cs
│   │
│   ├── iOS/
│   │   ├── Services/
│   │   │   ├── BluetoothService.cs  # iOS Bluetooth implementation
│   │   │   └── AudioService.cs      # iOS audio routing
│   │   ├── AppDelegate.cs
│   │   └── Program.cs
│   │
│   ├── Windows/
│   │   └── App.xaml / App.xaml.cs
│   │
│   └── MacCatalyst/
│       ├── AppDelegate.cs
│       └── Program.cs
│
├── Resources/                        # App resources
│   ├── AppIcon/
│   ├── Splash/
│   ├── Images/
│   ├── Fonts/
│   ├── Raw/
│   └── Styles/
│       ├── Colors.xaml
│       └── Styles.xaml
│
└── BluetoothMicrophoneApp.csproj    # Project configuration
```

---

## Key Components

### 1. IBluetoothService
**Location**: `Services/IBluetoothService.cs`

**Responsibilities:**
- Scanning for paired Bluetooth devices
- Connecting/disconnecting devices
- Device state management
- Event notifications for connection changes

**Platform Implementations:**
- Android: `Platforms/Android/Services/BluetoothService.cs`
- iOS: `Platforms/iOS/Services/BluetoothService.cs`

**Key Methods:**
```csharp
Task<List<BluetoothDevice>> ScanForDevicesAsync()
Task<bool> ConnectToDeviceAsync(BluetoothDevice device)
Task DisconnectAsync()
bool IsConnected { get; }
```

**Events:**
```csharp
event EventHandler<BluetoothDevice> DeviceConnected
event EventHandler DeviceDisconnected
```

### 2. IAudioService
**Location**: `Services/IAudioService.cs`

**Responsibilities:**
- Starting/stopping audio routing
- Managing AudioRecord (microphone input)
- Managing AudioTrack (audio output)
- Audio data flow in background thread

**Platform Implementations:**
- Android: `Platforms/Android/Services/AudioService.cs`
- iOS: `Platforms/iOS/Services/AudioService.cs`

**Key Methods:**
```csharp
Task<bool> StartAudioRoutingAsync()
Task StopAudioRoutingAsync()
bool IsRouting { get; }
```

**Android Implementation Details:**
- Sample Rate: 44100 Hz
- Format: PCM 16-bit mono
- Buffer Size: 2x minimum buffer size
- Audio Mode: `Mode.InCommunication`
- Uses Bluetooth SCO for audio routing
- Background thread for real-time processing

### 3. AudioFxEngine
**Location**: `Audio/AudioFxEngine.cs`

**Responsibilities:**
- Managing effect chain
- Processing audio through effects
- Loading/applying presets
- Real-time safe processing

**Key Features:**
- Effects can be bypassed individually
- Chain processing (output of one effect feeds next)
- Preset system for saving/loading configurations
- Prepare/Reset lifecycle management

**Usage Pattern:**
```csharp
var engine = new AudioFxEngine();
engine.Prepare(sampleRate: 44100, channels: 1);
engine.AddEffect(new GainEffect());
engine.AddEffect(new NoiseGateEffect());
engine.Process(audioBuffer); // In real-time loop
```

### 4. AudioBuffer
**Location**: `Audio/AudioBuffer.cs`

**Responsibilities:**
- Float32 audio buffer representation
- PCM16 ↔ Float32 conversion utilities
- Sample rate and channel tracking

**Format:**
- Internal format: Float32 [-1.0 to 1.0]
- Conversion support: Int16 PCM ↔ Float32

### 5. IConnectivityDiagnostics
**Location**: `Services/IConnectivityDiagnostics.cs`

**Responsibilities:**
- Bluetooth health checks
- Permission verification
- Device connection status
- Issue detection and recommendations

**Android Implementation:**
- Checks Bluetooth adapter state
- Verifies audio profiles (A2DP, Headset)
- Checks SCO connection state
- Validates microphone/Bluetooth permissions

---

## Audio Processing Pipeline

### Current Pipeline (Without Effects)

```
┌──────────────┐
│  Microphone  │
└──────┬───────┘
       │ PCM16 (byte[])
       │
       ▼
┌──────────────────┐
│  AudioRecord     │ (Android)
│  44100 Hz Mono   │
└──────┬───────────┘
       │ byte[] buffer (1024 bytes)
       │
       ▼
┌──────────────────┐
│ Direct Copy      │ (No processing)
└──────┬───────────┘
       │
       ▼
┌──────────────────┐
│  AudioTrack      │ (Android)
│  Bluetooth SCO   │
└──────┬───────────┘
       │
       ▼
┌──────────────────┐
│ Bluetooth Device │
│  (Headphones)    │
└──────────────────┘
```

### Intended Pipeline (With Effects) - TODO

```
┌──────────────┐
│  Microphone  │
└──────┬───────┘
       │ PCM16 (byte[])
       │
       ▼
┌──────────────────┐
│  AudioRecord     │
└──────┬───────────┘
       │
       ▼
┌──────────────────┐
│ PCM16 → Float32  │ AudioBuffer.Int16ToFloat()
└──────┬───────────┘
       │ Float32 [-1.0..1.0]
       │
       ▼
┌──────────────────┐
│  AudioFxEngine   │
│  ┌────────────┐  │
│  │ GainEffect │  │
│  └────┬───────┘  │
│       ▼          │
│  ┌────────────┐  │
│  │ NoiseGate  │  │
│  └────┬───────┘  │
│       ▼          │
│  ┌────────────┐  │
│  │ Compressor │  │
│  └────┬───────┘  │
│       ▼          │
│  ┌────────────┐  │
│  │  Limiter   │  │
│  └────────────┘  │
└──────┬───────────┘
       │ Float32 (processed)
       │
       ▼
┌──────────────────┐
│ Float32 → PCM16  │ AudioBuffer.FloatToInt16()
└──────┬───────────┘
       │ PCM16 (byte[])
       │
       ▼
┌──────────────────┐
│  AudioTrack      │
└──────┬───────────┘
       │
       ▼
┌──────────────────┐
│ Bluetooth Device │
└──────────────────┘
```

---

## Platform-Specific Implementations

### Android

**Bluetooth Service** (`Platforms/Android/Services/BluetoothService.cs`)
- Uses `BluetoothAdapter` for device scanning
- Only returns bonded (paired) devices
- Verifies device audio capabilities via `BluetoothClass`
- Checks `MajorDeviceClass.AudioVideo` for audio support

**Audio Service** (`Platforms/Android/Services/AudioService.cs`)
- `AudioRecord`: Captures microphone input
  - Source: `AudioSource.Mic`
  - Sample Rate: 44100 Hz
  - Format: PCM 16-bit mono
- `AudioTrack`: Plays audio through Bluetooth
  - Usage: `AudioUsageKind.VoiceCommunication`
  - Content: `AudioContentType.Speech`
- `AudioManager`:
  - Mode: `Mode.InCommunication`
  - `StartBluetoothSco()` / `StopBluetoothSco()`
- Background thread for continuous audio routing

**Connectivity Diagnostics** (`Platforms/Android/Services/ConnectivityDiagnostics.cs`)
- Bluetooth state verification
- Audio profile checks (A2DP, Headset)
- SCO connection status
- Permission validation

### iOS

**Status**: Implementations exist but specific details not fully explored in this session.

**Locations:**
- `Platforms/iOS/Services/BluetoothService.cs`
- `Platforms/iOS/Services/AudioService.cs`

---

## Current Status & TODOs

### ✅ Working Features
1. Bluetooth device scanning (manual + auto-scan every 30s)
2. Device connection/disconnection
3. Audio routing (microphone → Bluetooth)
4. UI with gain slider (visual only)
5. Diagnostics and debug logging
6. Permission handling

### 🔧 Known Issues & TODOs

#### 1. **Audio Effects Not Integrated** ⚠️ HIGH PRIORITY
**Location**: `MainPage.xaml.cs:506`
```csharp
// TODO: Apply gain to audio processing
// This will be integrated with the AudioFxEngine's GainEffect
```

**What's Missing:**
- AudioFxEngine is not instantiated in AudioService
- Gain slider changes don't affect actual audio
- No PCM16↔Float32 conversion in audio loop
- AudioBuffer not used in AudioRoutingLoop

**Required Changes:**
1. Modify `AudioService.AudioRoutingLoop()` to:
   - Convert PCM16 byte[] to Float32 using `AudioBuffer.Int16ToFloat()`
   - Process through `AudioFxEngine`
   - Convert back to PCM16 using `AudioBuffer.FloatToInt16()`
2. Wire up gain slider to `AudioFxEngine.GainEffect`
3. Add preset selection UI

#### 2. **No Preset Management UI**
- AudioPreset class exists
- No UI to save/load/apply presets
- No default presets defined

**Suggested Presets:**
- "Voice Boost" - Gain + Compressor + NoiseGate
- "Bass Boost" - EQ with low freq boost
- "Clean Voice" - NoiseGate + Compressor + Limiter
- "Echo Effect" - Echo + Gain

#### 3. **No Real-time Level Metering**
- No visual feedback of audio levels
- Could add VU meter or waveform visualization

#### 4. **iOS Implementation Unknown**
- iOS services exist but not tested
- May need iOS-specific audio routing approach

#### 5. **No Error Recovery**
- If Bluetooth disconnects during audio routing, no auto-reconnect
- Audio routing thread crash handling needs improvement

#### 6. **Buffer Size Not Optimized**
- Using fixed 1024-byte buffer
- Could be tuned for lower latency

---

## Development Guide

### Prerequisites
- Visual Studio 2022 (or later)
- .NET 9.0 SDK
- Android SDK (for Android development)
- Xcode (for iOS/macOS development)
- JDK 21 (specified in .csproj)

### Building the Project

**For Android:**
```bash
dotnet build -f net9.0-android
```

**For iOS:**
```bash
dotnet build -f net9.0-ios
```

**For Windows:**
```bash
dotnet build -f net9.0-windows10.0.19041.0
```

### Running on Device

**Android:**
```bash
dotnet run -f net9.0-android
```

### Key Configuration Files

**BluetoothMicrophoneApp.csproj**
- Target frameworks
- App metadata (name, ID, version)
- Supported OS versions
- Dependencies

**MauiProgram.cs**
- Service registration (DI container)
- Platform-specific service selection
- Logging configuration

### Debugging

**Debug Logs:**
- In-app: Tap "View Logs" button
- Uses `DebugLogger.Log()` for custom logging
- Android: View via `adb logcat`

**Diagnostics:**
- In-app: Tap "Diagnostics" button
- Shows connectivity health report
- Recommendations for fixing issues

### Adding New Audio Effects

1. **Create Effect Class** in `Audio/Effects/`
```csharp
public class MyEffect : IAudioEffect
{
    public string Name => "MyEffect";
    public bool Bypass { get; set; }

    public void Prepare(int sampleRate, int channels) { }
    public void Reset() { }
    public void Process(AudioBuffer buffer)
    {
        // Process buffer.Data (float[])
    }
    public void SetParameters(Dictionary<string, object> parameters) { }
}
```

2. **Register in AudioFxEngine** (`AudioFxEngine.cs:113`)
```csharp
private IAudioEffect? CreateEffect(string name)
{
    return name switch
    {
        // ... existing effects
        "MyEffect" => new Effects.MyEffect(),
        _ => null
    };
}
```

3. **Add to Preset** or UI

### UI Customization

**Color Scheme** (`Resources/Styles/Colors.xaml`)
- Background: `#1A1A2E` (dark blue-gray)
- Cards: `#2D2D44` (lighter gray)
- Primary: `#4A90E2` (blue)
- Success: `#4CAF50` (green)
- Error: `#FF5252` (red)
- Warning: `#FF9800` (orange)

**Design System**
- See `UI/DesignSystem.cs` for reusable UI components
- Custom dialog: `UI/CustomDialog.xaml`

---

## Next Steps / Recommendations

### Immediate (High Priority)
1. **Integrate AudioFxEngine with AudioService**
   - Modify AudioRoutingLoop to use Float32 processing
   - Wire up gain slider to actual audio processing
   - Test latency impact

2. **Add Default Presets**
   - Create 3-5 useful presets
   - Add preset selection UI
   - Save/load preset preferences

### Short-term
3. **Add Level Metering**
   - Real-time VU meter
   - Clip detection/warning
   - Input/output level display

4. **Improve Error Handling**
   - Auto-reconnect on Bluetooth disconnect
   - Better thread safety
   - Graceful degradation

### Long-term
5. **iOS Testing & Refinement**
   - Test on iOS devices
   - Verify audio routing works
   - Platform-specific optimizations

6. **Advanced Features**
   - Custom effect parameters UI
   - Recording functionality
   - EQ visualization
   - Multi-band compressor
   - Real-time spectral analysis

---

## License & Contact

**Developer**: Pen-Link Ltd
**App ID**: com.penlink.ezmiclink
**Version**: 1.0

---

**Document Version**: 1.0
**Last Updated**: February 19, 2026
**Generated by**: Claude Code (Sonnet 4.5)

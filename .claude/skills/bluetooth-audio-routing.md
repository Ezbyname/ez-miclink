# Skill: Bluetooth Audio Routing

## Overview
Routes real-time microphone audio to Bluetooth devices (headsets, speakers, car audio) on Android and iOS using native platform APIs. No third-party library needed.

## Key Facts
- **No NuGet package exists for Bluetooth A2DP audio streaming.** All .NET BT libraries (Plugin.BLE, Shiny.BluetoothLE, 32feet.NET) handle BLE data or RFCOMM sockets, NOT audio.
- AudioRecord + AudioTrack is the standard Android approach used by all commercial mic relay apps.
- The OS audio HAL handles Bluetooth codec negotiation (SBC, AAC, aptX, LDAC) transparently.

## Android: Two Audio Routing Paths

### Path 1: SCO (Headsets / Hands-Free)
- **Profile:** HFP (Hands-Free Profile)
- **Quality:** 8-16kHz mono, 64kbps (phone call quality)
- **Latency:** 20-50ms (lowest)
- **Setup:**
```csharp
_audioManager.Mode = Mode.InCommunication;
_audioManager.StartBluetoothSco();
// Wait for SCO_AUDIO_STATE_CONNECTED
// AudioTrack with AudioUsageKind.VoiceCommunication
```

### Path 2: A2DP (Speakers / Earbuds / Car Audio)
- **Profile:** A2DP (Advanced Audio Distribution)
- **Quality:** Up to 990kbps stereo (codec dependent)
- **Latency:** 100-250ms (codec dependent)
- **Setup:**
```csharp
_audioManager.Mode = Mode.Normal;
// AudioTrack with AudioUsageKind.Media + AudioContentType.Music
// CRITICAL: Use SetPreferredDevice() to target the A2DP device explicitly
var devices = _audioManager.GetDevices(GetDevicesTargets.Outputs);
foreach (var device in devices)
{
    if (device.Type == AudioDeviceType.BluetoothA2dp)
    {
        _audioTrack.SetPreferredDevice(device);
        break;
    }
}
```

### Detection Strategy
1. Check `AudioManager.IsBluetoothScoAvailableOffCall`
2. Try SCO first (3s timeout via BroadcastReceiver for `ActionScoAudioStateUpdated`)
3. If SCO fails, check `AudioManager.GetDevices(Outputs)` for `BluetoothA2dp` type
4. Fall back to phone speaker if neither available

### Critical APIs
- `AudioTrack.SetPreferredDevice(AudioDeviceInfo)` - explicitly route to A2DP device (API 23+)
- `AudioTrack.AddOnRoutingChangedListener()` - detect mid-stream disconnect
- Do NOT use deprecated `AudioManager.BluetoothA2dpOn` (ignored since API 28)

## Bluetooth Codec Latency Reference

| Codec | Latency | Quality | Notes |
|-------|---------|---------|-------|
| SBC | 150-250ms | Good | Universal default |
| AAC | 120-200ms | Very Good | Apple preferred |
| aptX | 60-80ms | Very Good | Qualcomm, lower latency |
| aptX LL | 32-40ms | Very Good | Best real-time, rare |
| LDAC | 160-210ms | Best | Sony, high quality |
| LC3 (LE Audio) | 20-30ms | Excellent | New standard |
| SCO/HFP | 20-50ms | Poor | Voice only, bidirectional |

## iOS: AVAudioEngine Approach
```swift
// Set audio session for Bluetooth
let session = AVAudioSession.sharedInstance()
try session.setCategory(.playAndRecord, mode: .default,
    options: [.allowBluetooth, .allowBluetoothA2DP])
try session.setActive(true)

// Use AVAudioEngine for real-time processing
let engine = AVAudioEngine()
let inputNode = engine.inputNode
let outputNode = engine.outputNode
// Tap input, process, route to output
```

## File Locations
- Android AudioService: `Platforms/Android/Services/AudioService.cs`
- iOS AudioService: `Platforms/iOS/Services/AudioService.cs`
- Interface: `Services/IAudioService.cs`

## Libraries Evaluated (Not Recommended for Audio)
| Library | NuGet | Purpose | Audio Support |
|---------|-------|---------|---------------|
| Plugin.BLE | Plugin.BLE | BLE scanning/GATT | None |
| Shiny.BluetoothLE | Shiny.Hosting.Maui | BLE | None |
| 32feet.NET | InTheHand.Net.Bluetooth | Classic BT RFCOMM | Socket only, no A2DP |
| Oboe (C++) | N/A | Low-latency audio | Overkill, BT latency dominates |

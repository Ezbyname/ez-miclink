# Architecture Patterns Analysis & Refactoring Plan

**Skill Applied**: [architecture-patterns](https://skills.sh/skill/wshobson/agents/architecture-patterns)

**Date**: 2026-02-25
**Status**: Analysis Complete, Refactoring In Progress

---

## 🎯 Executive Summary

This document analyzes the current architecture of BluetoothMicrophoneApp and provides a comprehensive refactoring plan based on clean architecture principles and SOLID design patterns.

**Current State**: Functional but coupled, difficult to test, limited extensibility
**Target State**: Clean architecture, testable, extensible, maintainable

---

## 📊 Current Architecture Issues

### 🔴 Critical Issues

#### 1. **Violation of Single Responsibility Principle (SRP)**

**Location**: `MainPage.xaml.cs` (800+ lines)

**Problem**: MainPage has too many responsibilities:
- UI rendering and animation
- Bluetooth device discovery and connection
- Audio service lifecycle management
- Permission handling
- State machine management
- Error handling and dialogs
- Navigation logic

**Impact**:
- Hard to test
- Changes in one area break others
- Difficult to understand
- High coupling

**Example**:
```csharp
public partial class MainPage : ContentPage
{
    // 10+ different responsibilities mixed together
    private readonly IBluetoothService _bluetoothService;
    private readonly IAudioService _audioService;
    private readonly IConnectivityDiagnostics _diagnostics;
    private List<BluetoothDevice> _availableDevices = new();
    private UIState _currentState = UIState.Initial;
    private CancellationTokenSource? _magnifyingGlassAnimationCts;
    // ... and much more
}
```

---

#### 2. **Violation of Open/Closed Principle (OCP)**

**Location**: `Audio/DSP/AudioEngine.cs` - `SetPreset()` method

**Problem**: Giant switch statement for presets (18+ cases)

**Code Smell**:
```csharp
public void SetPreset(string presetName)
{
    switch (presetName.ToLower())
    {
        case "podcast": BuildPodcastPreset(); break;
        case "stage_mc": BuildStageMCPreset(); break;
        case "karaoke": BuildKaraokePreset(); break;
        case "announcer": BuildAnnouncerPreset(); break;
        case "robot": BuildRobotPreset(); break;
        case "megaphone": BuildMegaphonePreset(); break;
        case "stadium": BuildStadiumPreset(); break;
        case "deep_voice": BuildDeepVoicePreset(); break;
        case "chipmunk": BuildChipmunkPreset(); break;
        case "anime": BuildAnimeVoicePreset(); break;
        case "nerdy": BuildNerdyVoicePreset(); break;
        // ... 8 more cases
        default: throw new ArgumentException($"Unknown preset: {presetName}");
    }
}
```

**Impact**:
- Adding new preset requires modifying AudioEngine
- Cannot extend presets without changing core code
- Tight coupling between engine and preset definitions
- Violates Open/Closed Principle

---

#### 3. **Missing Application Layer**

**Problem**: No clear separation between:
- **Presentation Layer** (UI/XAML)
- **Application Layer** (use cases/commands)
- **Domain Layer** (business logic)
- **Infrastructure Layer** (platform-specific)

**Current Structure**:
```
UI (MainPage) ────▶ Services (IAudioService, IBluetoothService)
                    └─▶ Platform Code (Android/iOS)
```

**Issues**:
- UI directly calls services (tight coupling)
- No business logic layer
- Cannot test use cases independently
- Hard to change UI framework

---

#### 4. **No Dependency Inversion**

**Problem**: High-level modules depend on low-level modules

**Example**:
- MainPage directly depends on concrete implementation details
- AudioEngine directly creates effect instances
- No abstraction for preset management

---

#### 5. **Tight Coupling Between Layers**

**Problem**: UI knows too much about services

**Example**:
```csharp
// MainPage.xaml.cs - UI layer directly manipulating services
private async void OnConnectClicked(object sender, EventArgs e)
{
    await _audioService.StartAudioRoutingAsync();
    _bluetoothService.ConnectToDeviceAsync(_selectedDevice);
    // UI logic mixed with service orchestration
}
```

---

### 🟡 Medium Priority Issues

#### 6. **No Command Pattern for User Actions**

**Problem**: Event handlers contain business logic

**Impact**:
- Cannot test user actions
- Hard to add undo/redo
- Difficult to log user interactions

---

#### 7. **State Management Scattered Across Code**

**Problem**: Multiple sources of truth:
- `_currentState` in MainPage
- `IsConnected` in BluetoothService
- `IsRouting` in AudioService
- Device connection history in Preferences

**Impact**:
- Race conditions
- Inconsistent state
- Hard to debug

---

#### 8. **No Repository Pattern**

**Problem**: Direct Preferences access scattered everywhere

**Example**:
```csharp
// Direct preferences access in multiple places
Preferences.Get("device_name", "Unknown");
Preferences.Set("noise_reduction", enabled);
```

**Impact**:
- Hard to test (dependency on platform)
- Cannot mock data access
- Difficult to change storage mechanism

---

#### 9. **No Mediator Pattern for Service Communication**

**Problem**: Services communicate through events and direct calls

**Impact**:
- Hard to trace interactions
- Tight coupling between services
- Difficult to add cross-cutting concerns (logging, validation)

---

#### 10. **Missing Factory Pattern for Effects**

**Problem**: AudioEngine directly instantiates effects

**Impact**:
- Cannot mock effects for testing
- Hard to add new effect types
- Tight coupling to concrete implementations

---

## 🏗️ Target Architecture (Clean Architecture)

### Layered Structure

```
┌─────────────────────────────────────────────────┐
│         PRESENTATION LAYER (MAUI)               │
│  MainPage, EffectsPage, ViewModels, Controls   │
└────────────────┬────────────────────────────────┘
                 │ (depends on)
┌────────────────▼────────────────────────────────┐
│         APPLICATION LAYER                       │
│  Use Cases, Commands, Queries, DTOs             │
│  - ConnectBluetoothDeviceUseCase                │
│  - StartAudioRoutingUseCase                     │
│  - ApplyAudioPresetUseCase                      │
└────────────────┬────────────────────────────────┘
                 │ (depends on)
┌────────────────▼────────────────────────────────┐
│         DOMAIN LAYER                            │
│  Entities, Value Objects, Domain Services       │
│  - AudioPreset (value object)                   │
│  - BluetoothDevice (entity)                     │
│  - IAudioPresetRepository (interface)           │
└────────────────┬────────────────────────────────┘
                 │ (implemented by)
┌────────────────▼────────────────────────────────┐
│         INFRASTRUCTURE LAYER                    │
│  Platform Code, Data Access, External Services  │
│  - AudioService (Android/iOS)                   │
│  - BluetoothService (Android/iOS)               │
│  - PreferencesRepository                        │
└─────────────────────────────────────────────────┘
```

---

## 🎯 SOLID Principles Application

### Single Responsibility Principle (SRP)

**Before**:
- MainPage does everything (800+ lines)

**After**:
- `MainPage` - Only UI rendering
- `MainPageViewModel` - Presentation logic
- `ConnectDeviceUseCase` - Connection business logic
- `BluetoothDeviceScanner` - Device discovery
- `AudioRoutingCoordinator` - Audio lifecycle

---

### Open/Closed Principle (OCP)

**Before**:
- Switch statement for presets (closed for extension)

**After**:
- Preset registry pattern (open for extension)

```csharp
public interface IAudioPreset
{
    string Name { get; }
    void Configure(AudioEffectChain chain, int sampleRate);
}

public class PresetRegistry
{
    private Dictionary<string, IAudioPreset> _presets = new();

    public void Register(IAudioPreset preset)
    {
        _presets[preset.Name] = preset;
    }

    public void Apply(string name, AudioEffectChain chain, int sampleRate)
    {
        if (_presets.TryGetValue(name, out var preset))
            preset.Configure(chain, sampleRate);
    }
}

// Adding new preset: ZERO changes to existing code
public class CustomPreset : IAudioPreset
{
    public string Name => "custom";
    public void Configure(AudioEffectChain chain, int sampleRate)
    {
        // Configure effects
    }
}
```

---

### Liskov Substitution Principle (LSP)

**After**:
- All presets implement `IAudioPreset`
- All effects implement `IAudioEffect`
- Can substitute any implementation

---

### Interface Segregation Principle (ISP)

**Before**:
- Fat interfaces (IAudioService does too much)

**After**:
- Split into focused interfaces:
  - `IAudioCapture` - Recording only
  - `IAudioPlayback` - Playback only
  - `IAudioEffectProcessor` - Effect processing
  - `IAudioLifecycle` - Start/stop

---

### Dependency Inversion Principle (DIP)

**Before**:
- High-level modules depend on low-level modules
- MainPage depends on concrete services

**After**:
- Both depend on abstractions
- Use cases define interfaces
- Infrastructure implements interfaces

---

## 📦 Design Patterns to Implement

### 1. **Repository Pattern**

**Purpose**: Abstract data access

**Implementation**:
```csharp
public interface IDeviceRepository
{
    Task<BluetoothDevice?> GetLastConnectedDeviceAsync();
    Task SaveDeviceAsync(BluetoothDevice device);
    Task<List<BluetoothDevice>> GetConnectionHistoryAsync();
}

public class PreferencesDeviceRepository : IDeviceRepository
{
    // Wraps Preferences API
}
```

**Benefits**:
- Testable (can mock)
- Can swap storage (SQLite, cloud, etc.)
- Centralized data access logic

---

### 2. **Factory Pattern**

**Purpose**: Create complex objects

**Implementation**:
```csharp
public interface IAudioEffectFactory
{
    IAudioEffect CreateEffect(string type);
}

public interface IAudioPresetFactory
{
    IAudioPreset CreatePreset(string name);
}
```

**Benefits**:
- Centralized creation logic
- Easy to mock for testing
- Can add caching, pooling

---

### 3. **Strategy Pattern**

**Purpose**: Algorithm selection at runtime

**Implementation**:
```csharp
public interface IAudioPreset
{
    string Name { get; }
    void Configure(AudioEffectChain chain, int sampleRate);
}

// Each preset is a strategy
public class PodcastPreset : IAudioPreset { }
public class RobotPreset : IAudioPreset { }
```

**Benefits**:
- Open/closed principle
- Easy to add new presets
- Testable in isolation

---

### 4. **Command Pattern**

**Purpose**: Encapsulate user actions

**Implementation**:
```csharp
public interface ICommand
{
    Task ExecuteAsync();
    bool CanExecute();
}

public class ConnectDeviceCommand : ICommand
{
    private readonly IBluetoothService _bluetooth;
    private readonly BluetoothDevice _device;

    public async Task ExecuteAsync()
    {
        await _bluetooth.ConnectToDeviceAsync(_device);
    }
}
```

**Benefits**:
- Undo/redo support
- Logging and auditing
- Queueing and throttling

---

### 5. **Mediator Pattern**

**Purpose**: Decouple service communication

**Implementation**:
```csharp
public interface IMediator
{
    Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request);
}

public class ConnectDeviceRequest : IRequest<ConnectDeviceResponse>
{
    public BluetoothDevice Device { get; set; }
}

public class ConnectDeviceHandler : IRequestHandler<ConnectDeviceRequest, ConnectDeviceResponse>
{
    public async Task<ConnectDeviceResponse> HandleAsync(ConnectDeviceRequest request)
    {
        // Coordinate services
    }
}
```

**Benefits**:
- Loose coupling
- Cross-cutting concerns (logging, validation)
- Testable handlers

---

### 6. **Observer Pattern (Already Using Events)**

**Current**: Events for status changes
**Improvement**: Use reactive extensions (System.Reactive)

---

### 7. **State Pattern**

**Purpose**: Manage UI state transitions

**Implementation**:
```csharp
public interface IConnectionState
{
    Task<IConnectionState> ScanAsync();
    Task<IConnectionState> ConnectAsync(BluetoothDevice device);
    Task<IConnectionState> DisconnectAsync();
}

public class DisconnectedState : IConnectionState { }
public class ScanningState : IConnectionState { }
public class ConnectedState : IConnectionState { }
```

**Benefits**:
- Clear state transitions
- Prevent invalid operations
- Easy to visualize flow

---

## 🔧 Refactoring Plan

### Phase 1: Foundation (Current Phase)

**Goal**: Set up architecture layers

#### Step 1.1: Create Domain Layer ✅ (This Task)

**New Folders**:
```
Domain/
├── Entities/
│   ├── BluetoothDevice.cs (move from Models/)
│   └── AudioSession.cs (new)
├── ValueObjects/
│   ├── AudioPreset.cs (new)
│   ├── DeviceAddress.cs (new)
│   └── SampleRate.cs (new)
├── Services/
│   └── IAudioPresetRegistry.cs (new)
└── Repositories/
    ├── IDeviceRepository.cs (new)
    └── IPresetRepository.cs (new)
```

#### Step 1.2: Create Application Layer ✅ (This Task)

**New Folders**:
```
Application/
├── UseCases/
│   ├── ConnectDevice/
│   │   ├── ConnectDeviceUseCase.cs
│   │   ├── ConnectDeviceRequest.cs
│   │   └── ConnectDeviceResponse.cs
│   ├── StartAudioRouting/
│   │   ├── StartAudioRoutingUseCase.cs
│   │   └── ...
│   └── ApplyAudioPreset/
│       ├── ApplyAudioPresetUseCase.cs
│       └── ...
├── Commands/
│   ├── ICommand.cs
│   └── CommandBase.cs
└── DTOs/
    ├── BluetoothDeviceDto.cs
    └── AudioStatusDto.cs
```

#### Step 1.3: Refactor AudioEngine with Preset Registry ✅ (This Task)

**Changes**:
1. Create `IAudioPreset` interface
2. Extract each preset to its own class
3. Create `PresetRegistry`
4. Replace switch statement with registry lookup

**Files to Create**:
```
Audio/Presets/
├── IAudioPreset.cs
├── PresetRegistry.cs
├── PodcastPreset.cs
├── RobotPreset.cs
├── MegaphonePreset.cs
└── ... (18 preset classes)
```

---

### Phase 2: Implement Repository Pattern (Next Session)

**Goal**: Abstract data access

#### Step 2.1: Create Repositories

```
Infrastructure/
└── Repositories/
    ├── PreferencesDeviceRepository.cs
    └── PreferencesPresetRepository.cs
```

#### Step 2.2: Replace Direct Preferences Access

**Current**:
```csharp
Preferences.Get("device_name", "Unknown");
```

**After**:
```csharp
await _deviceRepository.GetDeviceNameAsync(address);
```

---

### Phase 3: Implement MVVM Pattern (Future Session)

**Goal**: Separate presentation logic

#### Step 3.1: Create ViewModels

```
Presentation/
└── ViewModels/
    ├── MainPageViewModel.cs
    ├── EffectsPageViewModel.cs
    └── SettingsPageViewModel.cs
```

#### Step 3.2: Extract Business Logic from Code-Behind

Move all logic from `MainPage.xaml.cs` to `MainPageViewModel.cs`

---

### Phase 4: Implement Mediator Pattern (Future Session)

**Goal**: Decouple services

Use MediatR library or custom implementation

---

## 📊 Expected Benefits

### Testability

**Before**: ~0% test coverage
**After**: >70% test coverage

### Maintainability

**Before**: Maintainability Index ~60
**After**: Maintainability Index >80

### Extensibility

**Before**: Adding preset requires modifying AudioEngine
**After**: Add new preset class, register, done

### Performance

**No regression**: All changes maintain zero-allocation real-time audio path

---

## 🎯 Success Metrics

| Metric | Current | Target | Status |
|--------|---------|--------|--------|
| Code Duplication | ~15% | <5% | 🔄 In Progress |
| Maintainability Index | ~60 | >80 | 🔄 In Progress |
| Test Coverage | 0% | >70% | ⬜ Not Started |
| Cyclomatic Complexity | High (MainPage) | <10 avg | 🔄 In Progress |
| Lines per Method | ~50 avg | <20 avg | 🔄 In Progress |
| Dependencies per Class | ~5 avg | <3 avg | 🔄 In Progress |

---

## 🚀 Implementation Status

### ✅ Completed

- [x] Architecture analysis
- [x] Issue identification
- [x] Pattern selection
- [x] Refactoring plan creation

### 🔄 In Progress (Current Session)

- [ ] Create Domain layer structure
- [ ] Create Application layer structure
- [ ] Extract AudioPreset classes
- [ ] Implement PresetRegistry
- [ ] Refactor AudioEngine.SetPreset()

### ⬜ Pending (Future Sessions)

- [ ] Repository pattern implementation
- [ ] MVVM pattern implementation
- [ ] Mediator pattern implementation
- [ ] Unit test creation

---

## 📚 References

- **Clean Architecture**: Robert C. Martin (Uncle Bob)
- **SOLID Principles**: Robert C. Martin
- **Domain-Driven Design**: Eric Evans
- **Patterns of Enterprise Application Architecture**: Martin Fowler
- **Design Patterns**: Gang of Four

---

## 🎓 Key Takeaways

1. **Separation of Concerns**: Each class should have ONE reason to change
2. **Dependency Direction**: Always point inward (from Infrastructure → Domain)
3. **Abstraction**: Depend on interfaces, not concrete implementations
4. **Testability**: If it's hard to test, the architecture is wrong
5. **Extensibility**: Open for extension, closed for modification

---

## 💡 Next Steps

1. **Create Domain layer folders and files**
2. **Extract IAudioPreset interface**
3. **Create 18 preset classes (one per effect)**
4. **Implement PresetRegistry**
5. **Refactor AudioEngine to use registry**
6. **Add XML documentation**
7. **Build and verify zero errors**

**Estimated Time**: 2-3 hours for Phase 1

---

*Document will be updated as refactoring progresses.*

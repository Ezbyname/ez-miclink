# Claude Code Skill: Sound Effects Engine (Real-time Voice FX)
Version: 1.0
Purpose: Guide Claude Code to implement and extend a real-time sound effects pipeline for a "phone-as-mic" app (e.g., E-z MicLink).

---

## 0) What this skill should produce
Implement an **Audio FX Engine** that:
- Accepts live mic input frames (PCM float or int16).
- Applies a configurable **effect chain** (noise gate, EQ, compressor, limiter, reverb/echo, pitch/voice changer).
- Outputs processed audio frames for routing to speaker/stream.
- Supports **presets**, per-effect parameters, bypass, and automation-safe updates.
- Is stable under real-time constraints (no allocations/glitches).

Deliverables (minimum):
1. `AudioFxEngine` core with `process(buffer)` API
2. Effects: `Gain`, `EQ` (simple biquad), `Compressor`, `Limiter`, `Echo/Delay`, `NoiseGate`
3. Preset system (JSON) + `applyPreset(name)`
4. Unit tests for deterministic parts + basic perf checks

---

## 1) Real-time constraints (must-follow)
- **No heap allocations** in the audio callback / `process()` hot path.
- Use preallocated buffers, ring buffers, and object pools.
- Parameter updates must be **atomic** or lock-free (or use double-buffered params).
- Denormals: avoid CPU spikes (add tiny DC offset or flush-to-zero if available).
- Always clamp outputs to prevent clipping, and include limiter at end of chain by default.

---

## 2) Data format
Prefer internal processing in **float32 [-1..1]**.

### Buffer shape
- Interleaved stereo OR mono. Default: mono for mic input.
- Provide metadata: sampleRate, channels, frames.

Example struct (language-agnostic):
- `sampleRate: Int`
- `channels: Int`
- `frames: Int`
- `data: FloatArray` length = `frames * channels`

---

## 3) Architecture
### 3.1 Core interfaces
- `AudioEffect`
  - `prepare(sampleRate, channels)`
  - `reset()`
  - `process(inOutBuffer)` (in-place preferred)
  - `setParams(params)` (thread-safe handoff)
  - `bypass: Bool`

- `AudioFxEngine`
  - Holds ordered list: `effects[]`
  - `setChain(chainConfig)`
  - `applyPreset(preset)`
  - `process(buffer)`

### 3.2 Parameter update strategy
Use **double-buffered params**:
- UI thread writes to `pendingParams` (immutable snapshot)
- audio thread swaps pointer at buffer boundaries
- effects read params snapshot only (no locks)

---

## 4) Effects specs (minimum set)

### 4.1 Gain
- Param: `gainDb` (-24..+24)
- Convert: `linear = 10^(gainDb/20)`

### 4.2 Noise Gate
- Params: `thresholdDb` (-80..-20), `attackMs` (1..50), `releaseMs` (20..300)
- Use envelope follower on absolute signal:
  - env = max(abs(x), env * releaseCoeff) / attackCoeff variant
- If env below threshold => attenuate smoothly (not hard mute)

### 4.3 EQ (simple)
- 3-band EQ minimum (low shelf, peaking mid, high shelf)
- Params:
  - lowGainDb, midGainDb, highGainDb (-12..+12)
  - midFreqHz (200..3000), midQ (0.3..4.0)
- Implement using biquad filters (RBJ cookbook formulas)

### 4.4 Compressor
- Params:
  - thresholdDb (-40..0)
  - ratio (1..20)
  - attackMs (1..50), releaseMs (20..300)
  - makeupDb (0..24)
- Use RMS or envelope follower; compute gain reduction in dB.

### 4.5 Limiter (last in chain)
- Params:
  - ceilingDb (-6..0), releaseMs (10..200)
- Fast peak detection + gain smoothing.
- Hard requirement: prevent clipping.

### 4.6 Echo/Delay
- Params:
  - timeMs (40..600)
  - feedback (0..0.85)
  - mix (0..1)
- Implement ring buffer delay line per channel.
- Optional: ping-pong stereo if channels==2.

---

## 5) Presets
Presets are JSON objects defining chain + parameters.

### 5.1 Preset JSON format
```json
{
  "name": "Stage MC",
  "chain": ["NoiseGate", "EQ", "Compressor", "Echo", "Limiter"],
  "params": {
    "NoiseGate": {"thresholdDb": -45, "attackMs": 10, "releaseMs": 120},
    "EQ": {"lowGainDb": 3, "midGainDb": 2, "midFreqHz": 1200, "midQ": 1.0, "highGainDb": 2},
    "Compressor": {"thresholdDb": -18, "ratio": 4, "attackMs": 8, "releaseMs": 120, "makeupDb": 6},
    "Echo": {"timeMs": 140, "feedback": 0.25, "mix": 0.12},
    "Limiter": {"ceilingDb": -1.0, "releaseMs": 60}
  }
}
```

### 5.2 Required built-in presets
- **Podcast Clean** (gate + EQ + comp + limiter)
- **Stage MC** (brighter EQ + comp + light echo)
- **Karaoke** (gentle comp + larger echo)
- **Robot** (optional if you add ring-mod/vocoder later; otherwise omit)
- **Megaphone** (bandpass EQ + distortion optional)

---

## 6) Optional "cool" FX (nice-to-have)
Implement if time permits:
- **Distortion/Saturation** (soft clip tanh)
- **Pitch shift** (simple time-domain PSOLA or granular; quality can be basic)
- **Reverb** (Schroeder: comb + allpass) — keep CPU low
- **De-esser** (simple band detector around 5–8k + dynamic reduction)

---

## 7) Testing & Validation

### 7.1 Unit tests (deterministic)
- **Gain**: sine input amplitude change matches expected within epsilon.
- **Limiter**: input at +6dBFS never exceeds ceiling.
- **Echo**: impulse response matches ring-buffer timing.

### 7.2 Runtime checks
- **CPU budget**: process 10 seconds of audio under X ms on target device (set a reasonable threshold).
- **No allocations**: in debug builds, assert allocation counter doesn't increase in process loop (where possible).

---

## 8) Recommended file layout (adapt to repo)
```
audio/AudioFxEngine.*
audio/effects/Gain.*
audio/effects/NoiseGate.*
audio/effects/Biquad.* (shared)
audio/effects/EQ3Band.*
audio/effects/Compressor.*
audio/effects/Limiter.*
audio/effects/Echo.*
audio/presets/presets.json
audio/tests/*
```

---

## 9) Integration notes (Bluetooth mic apps)
Bluetooth output may add latency; provide:
- **"Latency Compensation"** setting (visual + measured where possible)
- Optional wired/USB fallback
- Add a **"Monitor"** toggle (local playback off by default to avoid feedback).
- Always include a big **Mute** and **Panic** (instant mute) function.

---

## 10) Acceptance criteria
The skill is complete when:
1. You can select a preset, speak into mic, and hear clearly processed output.
2. Changing parameters in UI updates audio smoothly (no clicks/pops).
3. Output never clips with limiter enabled.
4. Tests pass and the audio thread remains allocation-free.

---

## 11) If you (Claude Code) need clarification
If the repo already uses a specific stack, match it:
- **iOS**: AVAudioEngine / AudioUnit
- **Android**: Oboe / AAudio / AudioTrack
- **Cross-platform**: JUCE / Flutter (native plugin) / RN (native module)

If no stack is specified, implement the engine as a pure library with a minimal demo harness.

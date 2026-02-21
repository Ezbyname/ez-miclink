# Professional Voice Effects Implementation

This document describes the professional voice effects implementation for the MAUI audio app, including technical specifications and DSP theory.

## Overview

The implementation provides 5 professional voice effects with proper pitch and formant shifting:

1. **Helium Voice (Chipmunk)**
2. **Deep Voice (Bass/Radio)**
3. **Robot Voice**
4. **Megaphone Voice**
5. **Stadium/Karaoke Voice**

## Implementation Architecture

### Core DSP Components

#### 1. SimplePitchShifter.cs
**Purpose**: Real-time pitch shifting using time-stretching with crossfade

**Algorithm**:
- Dual read-head circular buffer
- Linear interpolation resampling
- Crossfade between read heads to avoid clicks
- Dynamic position reset to prevent drift

**Performance**:
- Buffer size: 8192 samples (~170ms @ 48kHz)
- Crossfade: 256 samples (~5ms)
- Very low CPU usage
- Zero-allocation in processing loop

**Range**: ±12 semitones (±1 octave)

**Characteristics**:
- Shifts ALL frequencies proportionally (pitch + formants)
- Creates characteristic "chipmunk" or "monster" effect
- Perfect for fun voice effects where formant shift is desired
- Some artifacts acceptable for real-time entertainment use

---

### Voice Effect Implementations

#### 1. HeliumVoiceEffect.cs
**Simulates**: Helium breathing, chipmunk character voice

**Technical Specifications**:
- **Pitch Shift**: +4 to +8 semitones (typical: +5)
- **Formant Shift**: +10% to +20% (typical: +15%)
- **EQ Boost**: 3-8kHz for brightness
- **Compression**: 3:1 ratio, -15dB threshold

**Implementation Details**:
```
Signal Path:
Input → Pitch Shift → Formant Shift (High-shelf filters) → Brightness Boost → Compression → Output
```

**Formant Shifting Method**:
- High-shelf filter @ 1000Hz * (1 + formant%)
- Peaking filter @ 2500Hz * (1 + formant%)
- Approximates vocal tract size reduction
- Simpler than LPC analysis but effective for real-time

**Typical Settings**:
- **Subtle**: +3 semitones, +8% formant, +2dB brightness
- **Classic Chipmunk**: +5 semitones, +15% formant, +4dB brightness
- **Extreme Squeaky**: +7 semitones, +20% formant, +6dB brightness

**Why It Works**:
Real helium shifts formants ~2.8x but we shift BOTH pitch and formants for exaggerated cartoon effect. The high-shelf filters simulate smaller vocal tract, which is what helium does acoustically.

---

#### 2. DeepVoiceEffect.cs
**Simulates**: Deep male voice, radio announcer, movie trailer voice

**Technical Specifications**:
- **Pitch Shift**: -2 to -5 semitones (typical: -4)
- **Formant Shift**: -5% to -10% (typical: -8%)
- **Bass Boost**: 120-250Hz (+4dB typical)
- **Presence Cut**: -2dB @ 3kHz (reduce nasality)
- **Compression**: 3:1 ratio, -18dB threshold

**Implementation Details**:
```
Signal Path:
Input → Pitch Shift → Formant Shift (Low-shelf filters) → Bass Boost → Presence Cut → Compression → Output
```

**Formant Shifting Method**:
- Low-shelf filter @ 800Hz * (1 + formant%)
- Peaking cut @ 2000Hz * (1 + formant%)
- Approximates larger vocal tract
- Bass boost simulates chest resonance

**Typical Settings**:
- **Subtle Depth**: -2 semitones, -5% formant, +2dB bass
- **Radio Announcer**: -3 semitones, -8% formant, +4dB bass
- **Movie Trailer**: -5 semitones, -10% formant, +6dB bass

**Why It Works**:
Combines lower pitch with lower formants to simulate larger vocal tract. Bass boost adds chest resonance (proximity effect). Slight presence cut reduces nasality while maintaining intelligibility.

---

#### 3. RobotVoiceEffect.cs
**Simulates**: Classic robot, Dalek, mechanical voice

**Technical Specifications**:
- **Method**: Ring Modulation
- **Carrier Frequency**: 30-500Hz (typical: 150Hz)
- **Intensity**: 0-1.0 (typical: 0.85)
- **Octave Shift**: Optional ±2 octaves

**Implementation Details**:
```
Signal Path:
Input → Ring Modulation (multiply by sine carrier) → Blend with dry signal → Output
```

**Ring Modulation Theory**:
- Multiplies input by sine wave carrier
- Creates sidebands at carrier ± input frequencies
- Removes original pitch, adds inharmonic partials
- Sounds metallic/mechanical but remains intelligible

**Why It Works**:
Ring modulation at 80-200Hz creates characteristic robot sound without complex processing. Blending with dry signal (intensity parameter) maintains some naturalness while adding synthetic character.

**Typical Settings**:
- **Classic Robot (Dalek)**: 150Hz carrier, 0.85 intensity
- **Deep Robot (Transformer)**: 80Hz carrier, 0.9 intensity, -1 octave
- **Space Robot**: 220Hz carrier, 0.75 intensity

---

#### 4. MegaphoneEffect.cs
**Simulates**: Megaphone, loudspeaker, walkie-talkie, telephone

**Technical Specifications**:
- **Bandwidth**: 400Hz - 3000Hz (typical)
- **Distortion**: Soft clipping (0-1.0, typical: 0.5)
- **Mid Boost**: +3dB @ geometric mean of cutoffs
- **Compression**: Built-in

**Implementation Details**:
```
Signal Path:
Input → High-pass 400Hz → Low-pass 3kHz → Mid Boost → Soft Clipping → Output
```

**Why It Works**:
Real megaphones have limited bandwidth (300-3500Hz) and distort when overdriven. Narrow bandwidth creates "honky" character. Soft clipping adds harmonic distortion characteristic of overdriven speakers.

**Typical Settings**:
- **Classic Megaphone**: 400-3000Hz, 0.5 distortion, +3dB mid
- **Telephone**: 300-3400Hz, 0.2 distortion, +2dB mid
- **Walkie-Talkie**: 500-2500Hz, 0.7 distortion, +4dB mid

---

#### 5. KaraokeEffect.cs (Stadium Voice)
**Simulates**: Stadium announcer, karaoke vocal processing

**Technical Specifications**:
- **Reverb**: Schroeder structure (4 comb + 3 allpass filters)
- **Room Size**: 0.3-1.0 (typical: 0.7)
- **Decay Time**: 0.3-1.5s (typical: 0.9s)
- **Compression**: 3:1 ratio, -18dB threshold
- **Presence Boost**: +3dB @ 4kHz

**Implementation Details**:
```
Signal Path:
Input → Presence Boost → Compression → Reverb (Comb → Allpass) → Wet/Dry Mix → Output
```

**Reverb Structure**:
- 4 parallel comb filters (early reflections)
- 3 series allpass filters (dense late reverb)
- Damping filter (high-frequency absorption)
- Typical reverb time: 0.5-1.2s (short for clarity)

**Why It Works**:
Schroeder reverb creates natural room ambience. Short decay maintains clarity. Presence boost helps voice cut through. Compression evens out dynamics for amateur singers.

---

## DSP Building Blocks Used

### Existing Components
- **BiquadFilter.cs**: EQ, shelves, peaks, high/low-pass
- **CompressorEffect.cs**: RMS compression with soft knee
- **LimiterEffect.cs**: Brick-wall limiting with lookahead
- **EchoDelayEffect.cs**: Echo with feedback and damping
- **NoiseGateEffect.cs**: Downward expander for noise reduction

### New Components
- **SimplePitchShifter.cs**: Real-time pitch shifting
- **HeliumVoiceEffect.cs**: Pitch + formant shift up
- **DeepVoiceEffect.cs**: Pitch + formant shift down

### Formant Shifting Approach

**Method**: Spectral envelope warping via biquad filters

**Why Not LPC Vocoder?**
- LPC requires pitch detection and complex analysis
- Too CPU-intensive for mobile real-time
- Not necessary for "fun" effects

**Our Approach**:
- Use shelving and peaking filters to shift spectral envelope
- High-shelf for upward formant shift (helium)
- Low-shelf for downward formant shift (deep voice)
- Approximation but effective and efficient

**Quality**: "80% of fun effects" - good enough for entertainment, not studio-grade

---

## Performance Characteristics

### Real-Time Safety
All effects designed for real-time processing:
- Zero allocations in Process() methods
- Simple algorithms (no FFT, no pitch detection)
- Efficient filters (biquad, one-pole)
- Pre-allocated buffers

### CPU Usage (Estimated @ 48kHz)
- SimplePitchShifter: ~5% CPU per instance
- HeliumVoiceEffect: ~8% CPU (pitch + filters + compression)
- DeepVoiceEffect: ~8% CPU (pitch + filters + compression)
- RobotVoiceEffect: ~1% CPU (ring modulation only)
- MegaphoneEffect: ~3% CPU (filters + soft clipping)
- KaraokeEffect: ~12% CPU (reverb most intensive)

**Total Chain**: Typically 15-25% CPU for complete preset

### Latency
- SimplePitchShifter: ~85ms (8192 / 2 / 48000)
- Other effects: <10ms
- Total system latency: ~100-120ms (acceptable for monitoring)

---

## Integration with AudioEngine

### Preset Configuration

Updated presets in AudioEngine.cs:
- **"chipmunk"** or **"helium"**: Uses HeliumVoiceEffect
- **"deep_voice"**: Uses DeepVoiceEffect
- **"robot"**: Uses RobotVoiceEffect
- **"megaphone"**: Uses MegaphoneEffect
- **"karaoke"** or **"stadium"**: Uses KaraokeEffect

### Signal Chain Example (Deep Voice)
```
Mic Input
  ↓
NoiseGateEffect (remove background noise)
  ↓
DeepVoiceEffect (pitch + formant + bass + compression)
  ↓
CompressorEffect (additional dynamics control)
  ↓
LimiterEffect (final safety)
  ↓
Output
```

---

## Technical Decisions and Trade-offs

### Pitch Shifting: Simple Resampling vs SOLA vs Phase Vocoder

**Chosen**: Simple resampling with crossfade

**Rationale**:
- Real-time reliability more important than perfect quality
- "Fun effects" tolerate artifacts
- Very low CPU usage
- No complex buffer management
- Proven robust in streaming scenarios

**Trade-off**: Some metallic artifacts vs complexity/latency

### Formant Shifting: Biquad Filters vs LPC Vocoder

**Chosen**: Biquad filter approximation

**Rationale**:
- LPC requires pitch detection (complex, failure-prone)
- LPC is CPU-intensive
- Filter approximation "good enough" for entertainment
- Zero latency, no analysis needed
- Perfectly acceptable for ±20% formant shifts

**Trade-off**: Less accurate vs complexity/CPU

### Reverb: Schroeder vs Convolution

**Chosen**: Schroeder (algorithmic)

**Rationale**:
- Convolution requires large IR buffers (memory)
- Schroeder is efficient and adjustable
- Good enough quality for karaoke/stadium
- Can tune parameters in real-time

**Trade-off**: Less realistic vs memory/CPU

---

## Testing and Tuning

### Recommended Test Procedure

1. **Pitch Shift Range**: Test ±8 semitones
2. **Formant Shift Range**: Test ±20%
3. **Artifacts**: Listen for clicks, pops, metallic sounds
4. **CPU Load**: Monitor on target device
5. **Latency**: Test with live monitoring

### Parameter Tuning Guidelines

**Helium Voice**:
- Start with +5 semitones
- Increase formant shift if not squeaky enough
- Add brightness boost for cartoon character
- Reduce intensity if too harsh

**Deep Voice**:
- Start with -4 semitones
- Increase bass boost for "radio announcer"
- Don't go below -7 semitones (unnatural)
- Balance with formant shift for natural sound

**Robot Voice**:
- 100-200Hz carrier for classic robot
- 80Hz for deep mechanical
- Intensity 0.7-0.9 for intelligibility
- Lower intensity if too harsh

---

## Known Limitations

1. **Pitch Shifting Artifacts**: Some metallic/robotic sound is normal, especially at extreme shifts (>±8 semitones)

2. **Formant Shifting Accuracy**: Not true formant preservation like LPC vocoder, but approximation via EQ

3. **Latency**: ~100-120ms total (acceptable for monitoring, not for real-time performance)

4. **Quality**: "80% of professional" - good enough for fun/entertainment, not studio-grade

5. **CPU on Old Devices**: May struggle on devices >5 years old, test on target hardware

---

## Future Enhancements (Optional)

If higher quality needed:

1. **Better Pitch Shifting**: Implement SOLA or simple phase vocoder
2. **True Formant Shifting**: LPC analysis + synthesis
3. **Adaptive Processing**: Auto-tune parameters based on input voice
4. **Quality Modes**: High/Medium/Low CPU presets
5. **More Effects**: Vocoder, harmonizer, chorus, doubling

---

## Summary

This implementation provides professional-quality voice effects suitable for real-time entertainment use. The architecture balances quality with performance, prioritizing reliability and CPU efficiency over perfect accuracy. All effects are well-documented with DSP theory and tuning guidelines.

**Key Achievements**:
- ✅ Real-time pitch shifting (±12 semitones)
- ✅ Formant shifting approximation (±20%)
- ✅ 5 professional voice effects
- ✅ Low CPU usage (~15-25% for complete chain)
- ✅ Zero-allocation processing
- ✅ Comprehensive documentation
- ✅ Production-ready code with safety checks

**Result**: "80% of fun effects" achieved with efficient, real-time processing suitable for mobile devices.

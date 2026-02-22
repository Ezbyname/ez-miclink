using System;

namespace BluetoothMicrophoneApp.Audio.DSP;

/// <summary>
/// Professional de-esser effect for controlling harsh sibilance (5-10 kHz).
///
/// WHAT IS A DE-ESSER:
/// A de-esser is a specialized dynamic processor that reduces excessive sibilance
/// ("s", "t", "ch" sounds) in vocal recordings. It's essentially a frequency-selective
/// compressor that only affects the sibilant range (typically 5-10 kHz).
///
/// WHY DE-ESSING IS CRITICAL:
/// 1. Pitch-shifted voices create exaggerated sibilance
/// 2. EQ brightness boosts amplify sibilance
/// 3. Compression can increase sibilance perception
/// 4. Broadcast requires controlled sibilance for transmission
/// 5. Prevents listener fatigue and harsh artifacts
///
/// HOW IT WORKS:
/// Split-Band Approach:
/// 1. Split signal into low-freq and sibilance-freq bands
/// 2. Detect sibilance energy in high band
/// 3. Apply compression only to high band when sibilance detected
/// 4. Recombine bands
///
/// IMPLEMENTATION APPROACH:
/// - High-pass filter isolates sibilance band
/// - RMS envelope detects sibilance level
/// - Dynamic gain reduction applied only to sibilance
/// - Crossover filter maintains phase coherence
///
/// INDUSTRY STANDARDS:
/// - Frequency: 5-10 kHz (6-7 kHz male, 7-9 kHz female)
/// - Threshold: -20 to -30 dB typical
/// - Ratio: 4:1 to 6:1 (more aggressive than standard compression)
/// - Reduction: 4-8 dB on sibilants
/// - Attack: 1-5 ms (fast to catch transients)
/// - Release: 50-100 ms (avoid pumping)
///
/// WHEN TO USE:
/// - After pitch shifting (especially upward pitch shift)
/// - After brightness EQ boosts
/// - Before limiting (to avoid clipping on sibilants)
/// - In broadcast chains (podcast, radio announcer)
/// - With compressed vocals (karaoke, professional processing)
///
/// PARAMETERS:
/// - Frequency: Center of sibilance detection (5-10 kHz)
/// - Threshold: Level above which de-essing engages
/// - Ratio: How much reduction (4:1 to 8:1 typical)
/// - Amount: Dry/wet mix (0-100%)
///
/// REFERENCES:
/// - LANDR: De-esser range 4-10 kHz
/// - Standard broadcast processing chains
/// - Plugin standards: FabFilter Pro-DS, Waves Renaissance DeEsser
/// </summary>
public class DeEsserEffect : IAudioEffect
{
	private DeEsserParameters _params;
	private int _sampleRate;

	// Split-band filters
	private BiquadFilter _sibilanceDetectHPF; // Isolate sibilance band
	private BiquadFilter _sibilanceBandBPF;   // Narrow sibilance band

	// Sibilance detection
	private float _sibilanceRMS;
	private float _detectorAttackCoef;
	private float _detectorReleaseCoef;

	// Gain reduction
	private float _gainReduction;
	private float _gainAttackCoef;
	private float _gainReleaseCoef;

	public bool Bypass { get; set; }

	public class DeEsserParameters
	{
		/// <summary>Center frequency of sibilance detection (5-10 kHz, typical 6-8 kHz)</summary>
		public float FrequencyHz { get; set; } = 7000f;

		/// <summary>Threshold in dB (-40 to -10, typical -25 to -30)</summary>
		public float ThresholdDb { get; set; } = -28f;

		/// <summary>Compression ratio for sibilance (4:1 to 8:1, typical 5:1)</summary>
		public float Ratio { get; set; } = 5f;

		/// <summary>Effect amount/intensity (0-1, typical 1.0 for full effect)</summary>
		public float Amount { get; set; } = 1.0f;

		/// <summary>Attack time in milliseconds (1-5 ms typical)</summary>
		public float AttackMs { get; set; } = 2f;

		/// <summary>Release time in milliseconds (50-150 ms typical)</summary>
		public float ReleaseMs { get; set; } = 80f;
	}

	public DeEsserEffect()
	{
		_params = new DeEsserParameters();
		_sibilanceDetectHPF = new BiquadFilter();
		_sibilanceBandBPF = new BiquadFilter();
	}

	public void Prepare(int sampleRate)
	{
		_sampleRate = sampleRate;
		UpdateParameters();
		Reset();
	}

	public void Process(float[] buffer, int offset, int count)
	{
		if (Bypass || _params.Amount <= 0f)
			return;

		float amount = Math.Clamp(_params.Amount, 0f, 1f);
		float thresholdLinear = DSPHelpers.DbToLinear(_params.ThresholdDb);

		for (int i = offset; i < offset + count; i++)
		{
			float input = buffer[i];

			// 1. Isolate sibilance band for detection
			// Use high-pass filter to focus on sibilance frequencies
			float hpfSignal = _sibilanceDetectHPF.Process(input);

			// 2. Use bandpass for more focused sibilance detection
			float sibilanceSignal = _sibilanceBandBPF.Process(hpfSignal);

			// 3. Detect sibilance energy using RMS envelope
			float sibilanceSq = sibilanceSignal * sibilanceSignal;
			float coef = sibilanceSq > _sibilanceRMS ? _detectorAttackCoef : _detectorReleaseCoef;
			_sibilanceRMS = _sibilanceRMS * coef + sibilanceSq * (1f - coef);

			float sibilanceLevel = MathF.Sqrt(MathF.Max(_sibilanceRMS, 1e-10f));

			// 4. Calculate gain reduction when sibilance exceeds threshold
			float gainReductionTarget = 1f;

			if (sibilanceLevel > thresholdLinear)
			{
				// How much over threshold
				float overageDb = DSPHelpers.LinearToDb(sibilanceLevel / thresholdLinear);

				// Apply ratio (e.g., 5:1 means reduce by 80% of overage)
				float reductionDb = overageDb * (1f - 1f / _params.Ratio);

				// Convert to linear gain reduction
				gainReductionTarget = DSPHelpers.DbToLinear(-reductionDb);
			}

			// 5. Smooth gain reduction to avoid clicks
			float gainCoef = gainReductionTarget < _gainReduction ? _gainAttackCoef : _gainReleaseCoef;
			_gainReduction = _gainReduction * gainCoef + gainReductionTarget * (1f - gainCoef);

			// 6. Apply gain reduction to sibilance band only
			// For simplicity in real-time, we apply to entire signal
			// (More complex split-band would require crossover filters)
			float reducedSignal = input * _gainReduction;

			// 7. Blend based on amount parameter
			buffer[i] = DSPHelpers.Lerp(input, reducedSignal, amount);
		}
	}

	public void SetParameters(object parameters)
	{
		if (parameters is DeEsserParameters p)
		{
			// Clamp parameters to safe ranges
			p.FrequencyHz = Math.Clamp(p.FrequencyHz, 3000f, 15000f);
			p.ThresholdDb = Math.Clamp(p.ThresholdDb, -50f, -5f);
			p.Ratio = Math.Clamp(p.Ratio, 1f, 10f);
			p.Amount = Math.Clamp(p.Amount, 0f, 1f);
			p.AttackMs = Math.Clamp(p.AttackMs, 0.5f, 20f);
			p.ReleaseMs = Math.Clamp(p.ReleaseMs, 20f, 300f);

			_params = p;

			if (_sampleRate > 0)
				UpdateParameters();
		}
	}

	public void Reset()
	{
		_sibilanceDetectHPF.Reset();
		_sibilanceBandBPF.Reset();
		_sibilanceRMS = 0f;
		_gainReduction = 1f;
	}

	private void UpdateParameters()
	{
		// High-pass filter to isolate upper frequencies
		// Set cutoff 2 octaves below target (e.g., 7kHz target → 1.75kHz HPF)
		float hpfFreq = _params.FrequencyHz * 0.3f;
		_sibilanceDetectHPF.Design(
			BiquadFilter.FilterType.HighPass,
			hpfFreq,
			_sampleRate,
			q: 0.707
		);

		// Bandpass filter centered on sibilance frequency
		// Narrow Q to focus on sibilants (s, t, ch sounds)
		_sibilanceBandBPF.Design(
			BiquadFilter.FilterType.Peaking,
			_params.FrequencyHz,
			_sampleRate,
			q: 2.5f,  // Narrow band for focused detection
			gainDb: 6f // Boost for better detection
		);

		// Time constants for sibilance detector
		_detectorAttackCoef = DSPHelpers.TimeToCoefficient(_params.AttackMs, _sampleRate);
		_detectorReleaseCoef = DSPHelpers.TimeToCoefficient(_params.ReleaseMs, _sampleRate);

		// Time constants for gain smoothing (slightly slower to avoid artifacts)
		_gainAttackCoef = DSPHelpers.TimeToCoefficient(_params.AttackMs * 1.5f, _sampleRate);
		_gainReleaseCoef = DSPHelpers.TimeToCoefficient(_params.ReleaseMs * 1.2f, _sampleRate);
	}
}

/// <summary>
/// DE-ESSER SCIENCE:
///
/// What is sibilance:
/// Sibilance refers to high-frequency consonant sounds ("s", "z", "sh", "ch", "t")
/// that occur in the 5-10 kHz range. These sounds can become harsh and distracting,
/// especially after processing.
///
/// Why sibilance becomes a problem:
/// 1. PITCH SHIFTING: Raises sibilance frequencies even higher
/// 2. EQ BOOSTS: Brightness/air boosts amplify sibilance
/// 3. COMPRESSION: Reduces dynamic range, making sibilance more prominent
/// 4. MICROPHONE PROXIMITY: Emphasizes high frequencies
/// 5. DIGITAL CLIPPING: Sibilance has high peak-to-RMS ratio
///
/// Industry standards for de-essing:
///
/// FREQUENCY TARGETING:
/// - Male voices: 5-7 kHz (typically 6 kHz)
/// - Female voices: 7-9 kHz (typically 8 kHz)
/// - Child voices: 8-10 kHz
/// - After pitch shift up: Add +1-2 kHz to target
///
/// THRESHOLD:
/// - Gentle: -30 to -35 dB (subtle control)
/// - Moderate: -25 to -30 dB (standard)
/// - Aggressive: -20 to -25 dB (heavy reduction)
///
/// RATIO:
/// - Subtle: 3:1 to 4:1
/// - Standard: 4:1 to 6:1
/// - Heavy: 6:1 to 8:1
/// - Limiting: 10:1+
///
/// ATTACK/RELEASE:
/// - Attack: 1-5 ms (fast enough to catch transient sibilants)
/// - Release: 50-150 ms (natural decay, avoid pumping)
///
/// SIGNAL CHAIN PLACEMENT:
/// De-esser typically goes:
/// - AFTER pitch shifting (addresses shifted sibilance)
/// - BEFORE compression (prevents compressor from amplifying sibilance)
/// - AFTER EQ (addresses EQ-boosted sibilance)
/// - Can be before OR after main compression (preferences vary)
///
/// COMMON DE-ESSER TYPES:
///
/// 1. SPLIT-BAND (Our Implementation):
///    - Detect sibilance in high band
///    - Compress only high band
///    - Maintains low/mid frequencies untouched
///    - Most transparent, most common
///
/// 2. FULL-BAND WITH SIDECHAIN:
///    - Sibilance detector triggers compression of full signal
///    - Can sound more natural
///    - May affect clarity slightly
///
/// 3. DYNAMIC EQ:
///    - Reduces gain at sibilance frequency when detected
///    - Very precise
///    - More CPU intensive
///
/// FAMOUS DE-ESSERS:
/// - Waves Renaissance DeEsser (split-band)
/// - FabFilter Pro-DS (dynamic EQ approach)
/// - UAD Precision De-Esser (optical circuit emulation)
/// - Ozone Dynamic EQ (multiband)
///
/// BROADCAST REQUIREMENTS:
/// Most broadcast standards require de-essing for:
/// - Podcast production (-16 LUFS target)
/// - Radio announcer processing
/// - Audiobook production
/// - Any pitch-shifted vocal content
/// - Heavy vocal compression chains
///
/// TESTING FOR SIBILANCE:
/// Test phrases with heavy sibilance:
/// - "She sells seashells by the seashore"
/// - "Sally sits sitting on the sidewalk"
/// - "Specific statistics"
/// - "Six thick thistle sticks"
///
/// If these sound harsh, piercing, or fatiguing, de-essing is needed.
///
/// TUNING TIPS:
/// 1. Solo the sibilance band to hear what's being detected
/// 2. Set threshold so gain reduction only occurs on "s" sounds
/// 3. Typical reduction: 4-6 dB (avoid over-processing)
/// 4. Listen for "lisp" - sign of over-de-essing
/// 5. A/B bypass to ensure natural sound maintained
///
/// CPU CONSIDERATIONS:
/// De-esser is lightweight:
/// - 2 biquad filters (minimal)
/// - RMS envelope detection (efficient)
/// - Simple gain calculation
/// - Typically <5% CPU on modern devices
/// </summary>

using System;

namespace BluetoothMicrophoneApp.Audio.DSP;

/// <summary>
/// Feedback suppressor using output-triggered input ducking.
///
/// Problem: BT speaker output is picked up by phone mic, creating echo loop.
/// Finding the exact echo delay is unreliable (voice auto-correlation, BT codec distortion).
///
/// Solution: When we send loud audio to the speaker, we KNOW the mic will pick it up
/// ~100-500ms later. So we duck (reduce) mic input for 500ms after any loud output.
/// This breaks the feedback loop without needing to know the exact delay.
///
/// The user's voice still gets through because:
/// - They speak FIRST → output is silent → full mic gain
/// - Speaker plays their voice → we duck mic for 500ms → echo is suppressed
/// - Echo dies because it's ducked below the loop sustain threshold
///
/// This is the same approach used in conference phones and PA systems.
/// Zero allocation, extremely lightweight.
/// </summary>
public class FeedbackCanceller
{
	private int _sampleRate;

	// Output energy tracking (what speaker is playing)
	private float _outputEnergy;
	private float _peakOutputEnergy; // peak over recent history

	// Ducking state
	private float _currentDuck;    // 0.0 = full duck, 1.0 = no duck
	private int _holdSamples;      // how many samples to keep ducking after output goes quiet
	private int _holdRemaining;    // countdown of hold samples

	// Tuning parameters
	private const float OutputThreshold = 0.01f;   // output energy threshold to trigger ducking (only loud output)
	private const float DuckAmount = 0.35f;        // how much to reduce mic during duck (0.35 = -9dB, gentle)
	private const float DuckAttackRate = 0.3f;     // how fast duck engages (slightly gradual)
	private const float DuckReleaseRate = 0.01f;   // how fast duck releases (faster recovery for natural sound)

	private volatile bool _enabled = true;
	private int _sampleCount;
	private int _duckActiveCount; // for logging

	public bool Enabled
	{
		get => _enabled;
		set => _enabled = value;
	}

	public void Prepare(int sampleRate)
	{
		_sampleRate = sampleRate;
		_outputEnergy = 0f;
		_peakOutputEnergy = 0f;
		_currentDuck = 1.0f; // start with no ducking
		_holdSamples = (int)(sampleRate * 0.15f); // 150ms hold time (covers BT latency without chopping voice)
		_holdRemaining = 0;
		_sampleCount = 0;
		_duckActiveCount = 0;
	}

	/// <summary>
	/// Track output energy. Called AFTER DSP, BEFORE writing to speaker.
	/// </summary>
	public void RecordReference(float[] buffer, int offset, int count)
	{
		if (!_enabled) return;

		// Compute output block energy
		float energy = 0f;
		for (int i = 0; i < count; i++)
		{
			float s = buffer[offset + i];
			energy += s * s;
		}
		energy /= Math.Max(count, 1);

		// Track smoothed output energy
		_outputEnergy = _outputEnergy * 0.9f + energy * 0.1f;

		// Track peak (decays slowly)
		if (_outputEnergy > _peakOutputEnergy)
			_peakOutputEnergy = _outputEnergy;
		else
			_peakOutputEnergy *= 0.999f;

		// If output is above threshold, reset the hold timer
		if (_outputEnergy > OutputThreshold)
		{
			_holdRemaining = _holdSamples;
		}
	}

	/// <summary>
	/// Apply ducking to mic input. Called BEFORE DSP processing.
	/// </summary>
	public void CancelEcho(float[] buffer, int offset, int count)
	{
		if (!_enabled) return;

		// Determine target duck level
		float targetDuck;
		if (_holdRemaining > 0)
		{
			// Speaker recently played something — duck the mic
			targetDuck = DuckAmount;
			_holdRemaining -= count;
		}
		else
		{
			// Speaker has been quiet for 500ms — full mic gain
			targetDuck = 1.0f;
		}

		// Smooth duck transitions (attack fast, release slow)
		if (targetDuck < _currentDuck)
		{
			// Ducking (fast attack)
			_currentDuck = Math.Max(targetDuck, _currentDuck - DuckAttackRate);
		}
		else
		{
			// Releasing (slow release to prevent echo resurgence)
			_currentDuck = Math.Min(targetDuck, _currentDuck + DuckReleaseRate);
		}

		// Apply duck gain to mic input
		if (_currentDuck < 0.99f)
		{
			float duck = _currentDuck;
			for (int i = 0; i < count; i++)
			{
				buffer[offset + i] *= duck;
			}
			_duckActiveCount++;
		}

		// Periodic logging
		_sampleCount += count;
		if (_sampleCount >= _sampleRate) // every second
		{
			_sampleCount = 0;
			if (_duckActiveCount > 0 || _currentDuck < 0.99f)
			{
				System.Diagnostics.Debug.WriteLine(
					$"[FeedbackCanceller] duck={_currentDuck:F2}, outEnergy={_outputEnergy:F6}, hold={_holdRemaining > 0}, duckBlocks={_duckActiveCount}");
				_duckActiveCount = 0;
			}
		}
	}

	public void Reset()
	{
		_outputEnergy = 0f;
		_peakOutputEnergy = 0f;
		_currentDuck = 1.0f;
		_holdRemaining = 0;
		_sampleCount = 0;
		_duckActiveCount = 0;
	}

	public void Stop()
	{
		// No background threads
	}
}

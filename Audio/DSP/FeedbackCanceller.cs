using System;

namespace BluetoothMicrophoneApp.Audio.DSP;

/// <summary>
/// Feedback suppressor for Bluetooth speaker scenarios.
///
/// Problem: Phone mic picks up audio from nearby BT speaker, creating
/// a feedback loop that echoes and can build up to howling.
///
/// Approach: Energy-based feedback gate.
/// - Tracks output energy (what we send to speaker)
/// - Tracks input energy (what mic captures)
/// - When input energy closely tracks output energy after a delay,
///   it's feedback - apply gain reduction to break the loop
/// - Uses exponential smoothing for stability (no audio glitches)
///
/// This is lightweight enough for the real-time audio thread.
/// Zero-allocation in steady state.
/// </summary>
public class FeedbackCanceller
{
	private int _sampleRate;

	// Energy tracking (exponential moving averages)
	private float _outputEnergy;    // smoothed energy of what we sent to speaker
	private float _inputEnergy;     // smoothed energy of what mic captured
	private float _pureInputEnergy; // input energy before any suppression (for detection)

	// Feedback detection state
	private float _feedbackRatio;   // how much of input looks like feedback
	private float _currentGain;     // current suppression gain (1.0 = no suppression)
	private int _feedbackFrames;    // consecutive frames with feedback detected

	// Parameters
	private volatile bool _enabled = true;
	private const float EnergySmooth = 0.995f;   // smoothing factor for energy tracking
	private const float GainAttack = 0.05f;       // how fast gain drops (gentle)
	private const float GainRelease = 0.002f;     // how fast gain recovers (very slow)
	private const float FeedbackThreshold = 0.4f; // ratio threshold for feedback detection
	private const float MinGain = 0.15f;          // minimum gain (don't completely mute)

	// Monitoring
	private int _sampleCount;

	public bool Enabled
	{
		get => _enabled;
		set => _enabled = value;
	}

	public void Prepare(int sampleRate)
	{
		_sampleRate = sampleRate;
		_outputEnergy = 0f;
		_inputEnergy = 0f;
		_pureInputEnergy = 0f;
		_feedbackRatio = 0f;
		_currentGain = 1.0f;
		_feedbackFrames = 0;
		_sampleCount = 0;
	}

	/// <summary>
	/// Record the energy of what we're sending to the speaker.
	/// Call AFTER DSP processing, BEFORE writing to AudioTrack.
	/// </summary>
	public void RecordReference(float[] buffer, int offset, int count)
	{
		if (!_enabled) return;

		float energy = 0f;
		for (int i = 0; i < count; i++)
		{
			float s = buffer[offset + i];
			energy += s * s;
		}
		energy /= Math.Max(count, 1);

		// Smooth output energy tracking
		_outputEnergy = _outputEnergy * EnergySmooth + energy * (1f - EnergySmooth);
	}

	/// <summary>
	/// Detect and suppress feedback in the mic input.
	/// Call on mic input BEFORE DSP processing.
	///
	/// Detection logic:
	/// - If we recently sent loud audio to speaker, AND mic now picks up
	///   similar energy, it's likely feedback
	/// - Apply gradual gain reduction to break the feedback loop
	/// - Release gain slowly when feedback stops
	/// </summary>
	public void CancelEcho(float[] buffer, int offset, int count)
	{
		if (!_enabled) return;

		// Measure input energy
		float inputE = 0f;
		for (int i = 0; i < count; i++)
		{
			float s = buffer[offset + i];
			inputE += s * s;
		}
		inputE /= Math.Max(count, 1);
		_pureInputEnergy = _pureInputEnergy * EnergySmooth + inputE * (1f - EnergySmooth);

		// Feedback detection: compare input energy to recent output energy
		// If output was loud and input is also loud, it's likely feedback
		bool feedbackDetected = false;

		if (_outputEnergy > 1e-5f)
		{
			// Ratio of input to output energy
			// If mic is picking up the speaker, this will be consistently > 0
			float ratio = _pureInputEnergy / (_outputEnergy + 1e-8f);

			// Smooth the ratio
			_feedbackRatio = _feedbackRatio * 0.95f + ratio * 0.05f;

			// Feedback is when input energy tracks output energy
			// (ratio stays in a consistent range, not random)
			if (_feedbackRatio > FeedbackThreshold && _pureInputEnergy > 1e-4f)
			{
				feedbackDetected = true;
				_feedbackFrames++;
			}
			else
			{
				_feedbackFrames = Math.Max(0, _feedbackFrames - 1);
			}
		}
		else
		{
			_feedbackFrames = Math.Max(0, _feedbackFrames - 1);
		}

		// Adjust gain based on feedback detection
		if (feedbackDetected && _feedbackFrames > 3)
		{
			// Reduce gain to suppress feedback (gradual attack)
			_currentGain = Math.Max(MinGain, _currentGain - GainAttack);
		}
		else
		{
			// Slowly release gain back to 1.0
			_currentGain = Math.Min(1.0f, _currentGain + GainRelease);
		}

		// Apply gain to input buffer
		if (_currentGain < 0.99f)
		{
			float gain = _currentGain;
			for (int i = 0; i < count; i++)
			{
				buffer[offset + i] *= gain;
			}
		}

		// Periodic logging
		_sampleCount += count;
		if (_sampleCount >= _sampleRate) // log once per second
		{
			_sampleCount = 0;
			if (_currentGain < 0.95f)
			{
				System.Diagnostics.Debug.WriteLine(
					$"[FeedbackCanceller] ACTIVE: gain={_currentGain:F2}, ratio={_feedbackRatio:F3}, outE={_outputEnergy:F6}, inE={_pureInputEnergy:F6}");
			}
		}
	}

	/// <summary>
	/// Reset the suppressor state.
	/// </summary>
	public void Reset()
	{
		_outputEnergy = 0f;
		_inputEnergy = 0f;
		_pureInputEnergy = 0f;
		_feedbackRatio = 0f;
		_currentGain = 1.0f;
		_feedbackFrames = 0;
		_sampleCount = 0;
	}
}

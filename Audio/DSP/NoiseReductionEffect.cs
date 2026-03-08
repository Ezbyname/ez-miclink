using System;

namespace BluetoothMicrophoneApp.Audio.DSP;

/// <summary>
/// Real-time noise reduction using spectral subtraction.
///
/// Algorithm:
/// 1. Collect audio into overlapping frames (50% overlap, Hann window)
/// 2. FFT each frame to get magnitude + phase spectrum
/// 3. During silence (auto-detected), learn the noise floor profile
/// 4. During speech, subtract the noise floor from the magnitude spectrum
/// 5. Apply spectral floor to prevent "musical noise" artifacts
/// 6. IFFT back to time domain, overlap-add to reconstruct
///
/// Zero-allocation in steady-state (all buffers pre-allocated).
/// Thread-safe parameter updates via volatile fields.
/// </summary>
public class NoiseReductionEffect : IAudioEffect
{
	// Frame size must be power of 2 for FFT
	private const int FrameSize = 512;
	private const int HalfFrame = FrameSize / 2;
	private const int HopSize = FrameSize / 2; // 50% overlap

	private int _sampleRate;

	// Pre-allocated buffers (zero allocation in Process)
	private float[] _inputRing;       // circular input buffer
	private float[] _outputRing;      // circular output buffer
	private int _inputWritePos;
	private int _outputReadPos;
	private int _samplesUntilNextFrame;

	private float[] _frame;           // current frame for FFT
	private float[] _window;          // Hann window
	private float[] _realPart;        // FFT real
	private float[] _imagPart;        // FFT imaginary
	private float[] _magnitude;       // magnitude spectrum
	private float[] _phase;           // phase spectrum
	private float[] _noiseFloor;      // learned noise profile
	private float[] _prevOutput;      // previous frame output (for overlap-add)

	// Noise learning state
	private float[] _noiseAccum;      // accumulator for noise learning
	private int _noiseFrameCount;
	private bool _hasNoiseProfile;
	private float _signalEnergy;
	private float _smoothedEnergy;

	// Parameters (volatile for thread-safe updates from UI thread)
	private volatile float _reductionStrength = 1.0f; // 0=off, 1=normal, 2=aggressive
	private volatile float _spectralFloor = 0.1f;     // minimum gain to prevent musical noise
	private volatile float _speechThreshold = 0.02f;   // energy threshold for speech detection
	private volatile bool _autoLearn = true;           // continuously learn noise during silence

	public bool Bypass { get; set; }

	/// <summary>Reduction strength: 0.0 (off) to 2.0 (aggressive). Default 1.0.</summary>
	public float ReductionStrength
	{
		get => _reductionStrength;
		set => _reductionStrength = Math.Clamp(value, 0f, 2f);
	}

	/// <summary>Spectral floor: 0.01 to 0.5. Higher = less artifacts but less reduction. Default 0.1.</summary>
	public float SpectralFloor
	{
		get => _spectralFloor;
		set => _spectralFloor = Math.Clamp(value, 0.01f, 0.5f);
	}

	/// <summary>Speech detection threshold: 0.001 to 0.1. Default 0.02.</summary>
	public float SpeechThreshold
	{
		get => _speechThreshold;
		set => _speechThreshold = Math.Clamp(value, 0.001f, 0.1f);
	}

	public void Prepare(int sampleRate)
	{
		_sampleRate = sampleRate;

		_inputRing = new float[FrameSize * 4];
		_outputRing = new float[FrameSize * 4];
		_inputWritePos = 0;
		_outputReadPos = 0;
		_samplesUntilNextFrame = FrameSize; // fill first frame before processing

		_frame = new float[FrameSize];
		_window = new float[FrameSize];
		_realPart = new float[FrameSize];
		_imagPart = new float[FrameSize];
		_magnitude = new float[HalfFrame + 1];
		_phase = new float[HalfFrame + 1];
		_noiseFloor = new float[HalfFrame + 1];
		_prevOutput = new float[FrameSize];
		_noiseAccum = new float[HalfFrame + 1];
		_noiseFrameCount = 0;
		_hasNoiseProfile = false;
		_smoothedEnergy = 0f;

		// Pre-compute Hann window
		for (int i = 0; i < FrameSize; i++)
		{
			_window[i] = 0.5f * (1f - MathF.Cos(2f * MathF.PI * i / (FrameSize - 1)));
		}
	}

	public void Initialize(int sampleRate) => Prepare(sampleRate);

	public void Process(float[] buffer, int offset, int count)
	{
		if (Bypass || _reductionStrength < 0.01f)
			return;

		int ringLen = _inputRing.Length;

		for (int i = 0; i < count; i++)
		{
			// Write input sample to ring buffer
			_inputRing[_inputWritePos % ringLen] = buffer[offset + i];
			_inputWritePos++;
			_samplesUntilNextFrame--;

			// When we have enough samples, process a frame
			if (_samplesUntilNextFrame <= 0)
			{
				ProcessFrame();
				_samplesUntilNextFrame = HopSize;
			}

			// Read from output ring buffer
			if (_outputReadPos < _inputWritePos - FrameSize)
			{
				// Skip ahead if we're behind (startup)
				_outputReadPos = _inputWritePos - FrameSize;
			}

			if (_outputReadPos >= 0 && _hasNoiseProfile)
			{
				buffer[offset + i] = _outputRing[_outputReadPos % ringLen];
			}
			_outputReadPos++;
		}
	}

	private void ProcessFrame()
	{
		int ringLen = _inputRing.Length;
		int frameStart = _inputWritePos - FrameSize;

		// Copy frame from ring buffer and apply window
		float energy = 0f;
		for (int i = 0; i < FrameSize; i++)
		{
			float sample = _inputRing[(frameStart + i + ringLen * 4) % ringLen];
			_frame[i] = sample * _window[i];
			energy += sample * sample;
		}
		energy /= FrameSize;

		// Smooth energy for speech detection
		_smoothedEnergy = _smoothedEnergy * 0.95f + energy * 0.05f;

		// Forward FFT
		Array.Copy(_frame, _realPart, FrameSize);
		Array.Clear(_imagPart, 0, FrameSize);
		FFT(_realPart, _imagPart, false);

		// Extract magnitude and phase
		for (int i = 0; i <= HalfFrame; i++)
		{
			_magnitude[i] = MathF.Sqrt(_realPart[i] * _realPart[i] + _imagPart[i] * _imagPart[i]);
			_phase[i] = MathF.Atan2(_imagPart[i], _realPart[i]);
		}

		// Noise learning: update profile during silence
		float threshold = _speechThreshold;
		if (_smoothedEnergy < threshold)
		{
			// Silence detected - learn noise floor
			if (_autoLearn || !_hasNoiseProfile)
			{
				for (int i = 0; i <= HalfFrame; i++)
				{
					_noiseAccum[i] += _magnitude[i];
				}
				_noiseFrameCount++;

				if (_noiseFrameCount >= 8) // need at least 8 frames for stable estimate
				{
					for (int i = 0; i <= HalfFrame; i++)
					{
						float newFloor = _noiseAccum[i] / _noiseFrameCount;
						if (_hasNoiseProfile)
						{
							// Smooth update: 80% old, 20% new
							_noiseFloor[i] = _noiseFloor[i] * 0.8f + newFloor * 0.2f;
						}
						else
						{
							_noiseFloor[i] = newFloor;
						}
					}

					if (!_hasNoiseProfile)
					{
						_hasNoiseProfile = true;
						System.Diagnostics.Debug.WriteLine("[NoiseReduction] Noise profile learned");
					}

					// Reset accumulator
					Array.Clear(_noiseAccum, 0, _noiseAccum.Length);
					_noiseFrameCount = 0;
				}
			}
		}

		// Spectral subtraction: remove noise from magnitude spectrum
		if (_hasNoiseProfile)
		{
			float strength = _reductionStrength;
			float floor = _spectralFloor;

			for (int i = 0; i <= HalfFrame; i++)
			{
				// Subtract noise floor scaled by strength
				float cleaned = _magnitude[i] - _noiseFloor[i] * strength;

				// Apply spectral floor to prevent musical noise
				float minMag = _magnitude[i] * floor;
				_magnitude[i] = MathF.Max(cleaned, minMag);
			}
		}

		// Reconstruct complex spectrum from cleaned magnitude + original phase
		for (int i = 0; i <= HalfFrame; i++)
		{
			_realPart[i] = _magnitude[i] * MathF.Cos(_phase[i]);
			_imagPart[i] = _magnitude[i] * MathF.Sin(_phase[i]);
		}

		// Mirror for negative frequencies
		for (int i = 1; i < HalfFrame; i++)
		{
			_realPart[FrameSize - i] = _realPart[i];
			_imagPart[FrameSize - i] = -_imagPart[i];
		}

		// Inverse FFT
		FFT(_realPart, _imagPart, true);

		// Overlap-add with window
		int outStart = _inputWritePos - FrameSize;
		for (int i = 0; i < FrameSize; i++)
		{
			float sample = _realPart[i] * _window[i];
			int idx = (outStart + i + ringLen * 4) % ringLen;

			if (i < HopSize)
			{
				// Overlap region: add to previous frame's tail
				_outputRing[idx] = _prevOutput[HopSize + i] + sample;
			}
			else
			{
				// New region: just write
				_outputRing[idx] = sample;
			}

			// Save for next overlap
			_prevOutput[i] = sample;
		}
	}

	/// <summary>
	/// In-place Cooley-Tukey FFT (radix-2 DIT).
	/// Zero allocation - operates on pre-allocated arrays.
	/// </summary>
	private static void FFT(float[] real, float[] imag, bool inverse)
	{
		int n = real.Length;

		// Bit-reversal permutation
		int j = 0;
		for (int i = 0; i < n - 1; i++)
		{
			if (i < j)
			{
				(real[i], real[j]) = (real[j], real[i]);
				(imag[i], imag[j]) = (imag[j], imag[i]);
			}
			int k = n >> 1;
			while (k <= j)
			{
				j -= k;
				k >>= 1;
			}
			j += k;
		}

		// Butterfly computation
		float sign = inverse ? 1f : -1f;
		for (int len = 2; len <= n; len <<= 1)
		{
			float angle = sign * 2f * MathF.PI / len;
			float wR = MathF.Cos(angle);
			float wI = MathF.Sin(angle);

			for (int i = 0; i < n; i += len)
			{
				float curR = 1f, curI = 0f;
				for (int k = 0; k < len / 2; k++)
				{
					int u = i + k;
					int v = u + len / 2;

					float tR = curR * real[v] - curI * imag[v];
					float tI = curR * imag[v] + curI * real[v];

					real[v] = real[u] - tR;
					imag[v] = imag[u] - tI;
					real[u] += tR;
					imag[u] += tI;

					float newCurR = curR * wR - curI * wI;
					curI = curR * wI + curI * wR;
					curR = newCurR;
				}
			}
		}

		// Normalize for inverse FFT
		if (inverse)
		{
			float invN = 1f / n;
			for (int i = 0; i < n; i++)
			{
				real[i] *= invN;
				imag[i] *= invN;
			}
		}
	}

	/// <summary>
	/// Force re-learning the noise profile from the next silence period.
	/// Call this when the environment changes.
	/// </summary>
	public void ResetNoiseProfile()
	{
		_hasNoiseProfile = false;
		_noiseFrameCount = 0;
		Array.Clear(_noiseAccum, 0, _noiseAccum.Length);
		Array.Clear(_noiseFloor, 0, _noiseFloor.Length);
		System.Diagnostics.Debug.WriteLine("[NoiseReduction] Noise profile reset - will re-learn on next silence");
	}

	public void SetParameters(object parameters)
	{
		if (parameters is NoiseReductionParameters p)
		{
			ReductionStrength = p.Strength;
			SpectralFloor = p.Floor;
			SpeechThreshold = p.SpeechThreshold;
		}
	}

	public void Reset()
	{
		if (_inputRing != null) Array.Clear(_inputRing, 0, _inputRing.Length);
		if (_outputRing != null) Array.Clear(_outputRing, 0, _outputRing.Length);
		if (_prevOutput != null) Array.Clear(_prevOutput, 0, _prevOutput.Length);
		_inputWritePos = 0;
		_outputReadPos = 0;
		_samplesUntilNextFrame = FrameSize;
		ResetNoiseProfile();
	}

	public class NoiseReductionParameters
	{
		public float Strength { get; set; } = 1.0f;
		public float Floor { get; set; } = 0.1f;
		public float SpeechThreshold { get; set; } = 0.02f;
	}
}

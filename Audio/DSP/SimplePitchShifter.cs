using System;

namespace BluetoothMicrophoneApp.Audio.DSP;

/// <summary>
/// Real-time pitch shifter using dual-grain overlap-add with Hann windowing.
///
/// Uses two overlapping grains offset by 50%, each windowed with Hann.
/// Larger grain size (4096 samples ~85ms at 48kHz) avoids buzzy/hoarse artifacts.
/// Grains reset to staggered positions to avoid both reading identical audio.
/// </summary>
public class SimplePitchShifter
{
    private const int BUFFER_SIZE = 65536;
    private const int GRAIN_SIZE = 8192; // ~170ms at 48kHz - very smooth for voice
    private const int HALF_GRAIN = GRAIN_SIZE / 2;

    private float[] _buffer;
    private int _writePos;

    // Two grain read positions
    private float _grainPos0;
    private float _grainPos1;
    private int _grainPhase;

    // Pre-computed Hann window
    private float[] _hannWindow;

    private float _pitchRatio;

    public SimplePitchShifter()
    {
        _buffer = new float[BUFFER_SIZE];
        _hannWindow = new float[GRAIN_SIZE];
        _pitchRatio = 1.0f;

        for (int i = 0; i < GRAIN_SIZE; i++)
        {
            _hannWindow[i] = 0.5f * (1f - MathF.Cos(2f * MathF.PI * i / GRAIN_SIZE));
        }

        Reset();
    }

    public void SetPitchSemitones(float semitones)
    {
        semitones = Math.Clamp(semitones, -12f, 12f);
        _pitchRatio = MathF.Pow(2f, semitones / 12f);
    }

    public void SetPitchRatio(float ratio)
    {
        _pitchRatio = Math.Clamp(ratio, 0.5f, 2.0f);
    }

    public float GetPitchRatio() => _pitchRatio;

    public void Process(float[] buffer, int offset, int count)
    {
        if (MathF.Abs(_pitchRatio - 1.0f) < 0.001f)
            return;

        for (int i = offset; i < offset + count; i++)
        {
            // Write input to circular buffer
            _buffer[_writePos] = buffer[i];
            _writePos = (_writePos + 1) & (BUFFER_SIZE - 1);

            // Window phases for each grain
            int phase0 = _grainPhase;
            int phase1 = (_grainPhase + HALF_GRAIN) % GRAIN_SIZE;

            float win0 = _hannWindow[phase0];
            float win1 = _hannWindow[phase1];

            // Read from each grain with interpolation, apply window
            float sample0 = ReadInterpolated(_grainPos0) * win0;
            float sample1 = ReadInterpolated(_grainPos1) * win1;

            buffer[i] = sample0 + sample1;

            // Advance grain positions at pitch-shifted rate
            _grainPos0 += _pitchRatio;
            _grainPos1 += _pitchRatio;

            // Advance shared phase
            _grainPhase = (_grainPhase + 1) % GRAIN_SIZE;

            // Reset grain 0 at end of its cycle
            if (phase0 == GRAIN_SIZE - 1)
            {
                // Place grain 0 behind write head by a safe margin
                _grainPos0 = _writePos - GRAIN_SIZE * _pitchRatio;
                WrapPosition(ref _grainPos0);
            }

            // Reset grain 1 at end of its cycle (offset by half grain)
            if (phase1 == GRAIN_SIZE - 1)
            {
                // Place grain 1 at a different offset to avoid reading same region as grain 0
                _grainPos1 = _writePos - HALF_GRAIN * _pitchRatio;
                WrapPosition(ref _grainPos1);
            }
        }
    }

    public void Reset()
    {
        Array.Clear(_buffer, 0, _buffer.Length);
        _writePos = 0;
        _grainPos0 = BUFFER_SIZE - (int)(GRAIN_SIZE * 1.5f);
        _grainPos1 = BUFFER_SIZE - GRAIN_SIZE;
        _grainPhase = 0;
    }

    private float ReadInterpolated(float position)
    {
        WrapPosition(ref position);
        int idx0 = (int)position;
        int idx1 = (idx0 + 1) & (BUFFER_SIZE - 1);
        float frac = position - idx0;
        return _buffer[idx0] * (1f - frac) + _buffer[idx1] * frac;
    }

    private void WrapPosition(ref float pos)
    {
        while (pos < 0) pos += BUFFER_SIZE;
        while (pos >= BUFFER_SIZE) pos -= BUFFER_SIZE;
    }
}

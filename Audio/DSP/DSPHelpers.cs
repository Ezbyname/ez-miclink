using System;

namespace BluetoothMicrophoneApp.Audio.DSP;

/// <summary>
/// DSP utility functions for audio processing.
/// All methods are designed for real-time use with no allocations.
/// </summary>
public static class DSPHelpers
{
    private const float EPSILON = 1e-10f;

    /// <summary>
    /// Convert linear amplitude to decibels
    /// </summary>
    public static float LinearToDb(float linear)
    {
        return 20f * MathF.Log10(MathF.Max(linear, EPSILON));
    }

    /// <summary>
    /// Convert decibels to linear amplitude
    /// </summary>
    public static float DbToLinear(float db)
    {
        return MathF.Pow(10f, db / 20f);
    }

    /// <summary>
    /// Calculate time constant coefficient for exponential smoothing.
    /// This is the core of envelope followers and smooth parameter changes.
    ///
    /// Formula: coef = exp(-1 / (timeMs * sampleRate / 1000))
    ///
    /// This gives approximately 63% of target value after timeMs milliseconds.
    /// </summary>
    public static float TimeToCoefficient(float timeMs, int sampleRate)
    {
        return MathF.Exp(-1f / (timeMs * sampleRate / 1000f));
    }

    /// <summary>
    /// Fast soft clipping using tanh approximation.
    /// Input range: unlimited, Output range: [-1, 1]
    /// Smoother than hard clipping, prevents harsh distortion.
    /// </summary>
    public static float SoftClip(float sample)
    {
        // Fast tanh approximation: x / (1 + |x|)
        // More accurate: return MathF.Tanh(sample);
        return sample / (1f + MathF.Abs(sample));
    }

    /// <summary>
    /// Hard clipping with safety margin
    /// </summary>
    public static float HardClip(float sample, float ceiling = 0.99f)
    {
        return MathF.Max(-ceiling, MathF.Min(ceiling, sample));
    }

    /// <summary>
    /// RMS (Root Mean Square) calculation for power/loudness detection.
    /// More accurate than peak detection for dynamic processing.
    /// </summary>
    public static float CalculateRMS(float[] buffer, int offset, int count)
    {
        float sum = 0f;
        for (int i = offset; i < offset + count; i++)
        {
            float sample = buffer[i];
            sum += sample * sample;
        }
        return MathF.Sqrt(sum / count);
    }

    /// <summary>
    /// Peak detection - finds maximum absolute value
    /// </summary>
    public static float CalculatePeak(float[] buffer, int offset, int count)
    {
        float peak = 0f;
        for (int i = offset; i < offset + count; i++)
        {
            float abs = MathF.Abs(buffer[i]);
            if (abs > peak) peak = abs;
        }
        return peak;
    }

    /// <summary>
    /// Linear interpolation
    /// </summary>
    public static float Lerp(float a, float b, float t)
    {
        return a + (b - a) * t;
    }

    /// <summary>
    /// Smooth parameter update using exponential smoothing.
    /// Use this to prevent zipper noise when changing parameters.
    ///
    /// current = current * coef + target * (1 - coef)
    ///
    /// Typical smoothing time: 10-20ms
    /// </summary>
    public static float SmoothParameter(float current, float target, float coefficient)
    {
        return current * coefficient + target * (1f - coefficient);
    }

    /// <summary>
    /// Apply makeup gain to compensate for level reduction in dynamics processors
    /// </summary>
    public static float CalculateMakeupGain(float thresholdDb, float ratio)
    {
        // Auto makeup gain formula: compensate for threshold and ratio
        // This brings the processed signal back to roughly original level
        float reductionDb = thresholdDb * (1f - 1f / ratio);
        return -reductionDb * 0.5f; // Conservative estimate
    }
}

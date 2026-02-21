using System;

namespace BluetoothMicrophoneApp.Audio.DSP;

/// <summary>
/// Biquad filter implementation using RBJ (Robert Bristow-Johnson) cookbook formulas.
///
/// THEORY:
/// A biquad (bi-quadratic) filter is a second-order IIR (Infinite Impulse Response) filter.
/// It's the building block for EQ, shelves, and most digital filters.
///
/// Transfer function:
/// H(z) = (b0 + b1*z^-1 + b2*z^-2) / (a0 + a1*z^-1 + a2*z^-2)
///
/// Difference equation:
/// y[n] = (b0*x[n] + b1*x[n-1] + b2*x[n-2] - a1*y[n-1] - a2*y[n-2]) / a0
///
/// WHY BIQUADS:
/// 1. Numerically stable
/// 2. Efficient (5 multiplies, 4 adds per sample)
/// 3. Versatile (can create any 2nd-order filter)
/// 4. Cascadable for higher-order filters
///
/// STABILITY:
/// Biquads are stable if poles stay inside unit circle.
/// This is guaranteed if we:
/// 1. Use double precision for coefficient calculation
/// 2. Normalize by a0
/// 3. Clamp Q values to reasonable ranges
/// </summary>
public class BiquadFilter
{
    // Filter coefficients (normalized by a0)
    private float _b0, _b1, _b2;
    private float _a1, _a2;

    // Filter state (previous inputs and outputs)
    private float _x1, _x2; // x[n-1], x[n-2]
    private float _y1, _y2; // y[n-1], y[n-2]

    public enum FilterType
    {
        LowPass,
        HighPass,
        BandPass,
        Notch,
        Peaking,
        LowShelf,
        HighShelf,
        AllPass
    }

    /// <summary>
    /// Design a biquad filter using RBJ cookbook formulas.
    ///
    /// PARAMETERS:
    /// - type: Filter type
    /// - freq: Center frequency in Hz
    /// - sampleRate: Sample rate in Hz
    /// - q: Quality factor (bandwidth) - typically 0.5 to 10
    /// - gainDb: Gain in dB (for peaking and shelf filters)
    ///
    /// Q VALUES:
    /// Q = 0.707 (√2/2): Butterworth (maximally flat)
    /// Q = 1.0: Standard Q for most applications
    /// Q > 1: Narrower bandwidth (more resonant)
    /// Q < 0.5: Wider bandwidth (gentler slope)
    /// </summary>
    public void Design(FilterType type, double freq, int sampleRate, double q = 1.0, double gainDb = 0.0)
    {
        // Clamp parameters to prevent instability
        freq = Math.Clamp(freq, 20.0, sampleRate / 2.5); // Leave margin below Nyquist
        q = Math.Clamp(q, 0.1, 20.0);

        // Calculate intermediate values
        double omega = 2.0 * Math.PI * freq / sampleRate;
        double sn = Math.Sin(omega);
        double cs = Math.Cos(omega);
        double alpha = sn / (2.0 * q);
        double A = Math.Pow(10.0, gainDb / 40.0); // sqrt of linear gain
        double beta = Math.Sqrt(A) / q;

        // RBJ cookbook formulas
        // Calculate coefficients based on filter type
        double a0, a1, a2, b0, b1, b2;

        switch (type)
        {
            case FilterType.LowPass:
                b0 = (1.0 - cs) / 2.0;
                b1 = 1.0 - cs;
                b2 = (1.0 - cs) / 2.0;
                a0 = 1.0 + alpha;
                a1 = -2.0 * cs;
                a2 = 1.0 - alpha;
                break;

            case FilterType.HighPass:
                b0 = (1.0 + cs) / 2.0;
                b1 = -(1.0 + cs);
                b2 = (1.0 + cs) / 2.0;
                a0 = 1.0 + alpha;
                a1 = -2.0 * cs;
                a2 = 1.0 - alpha;
                break;

            case FilterType.BandPass:
                b0 = alpha;
                b1 = 0.0;
                b2 = -alpha;
                a0 = 1.0 + alpha;
                a1 = -2.0 * cs;
                a2 = 1.0 - alpha;
                break;

            case FilterType.Notch:
                b0 = 1.0;
                b1 = -2.0 * cs;
                b2 = 1.0;
                a0 = 1.0 + alpha;
                a1 = -2.0 * cs;
                a2 = 1.0 - alpha;
                break;

            case FilterType.Peaking:
                b0 = 1.0 + alpha * A;
                b1 = -2.0 * cs;
                b2 = 1.0 - alpha * A;
                a0 = 1.0 + alpha / A;
                a1 = -2.0 * cs;
                a2 = 1.0 - alpha / A;
                break;

            case FilterType.LowShelf:
                b0 = A * ((A + 1.0) - (A - 1.0) * cs + beta * sn);
                b1 = 2.0 * A * ((A - 1.0) - (A + 1.0) * cs);
                b2 = A * ((A + 1.0) - (A - 1.0) * cs - beta * sn);
                a0 = (A + 1.0) + (A - 1.0) * cs + beta * sn;
                a1 = -2.0 * ((A - 1.0) + (A + 1.0) * cs);
                a2 = (A + 1.0) + (A - 1.0) * cs - beta * sn;
                break;

            case FilterType.HighShelf:
                b0 = A * ((A + 1.0) + (A - 1.0) * cs + beta * sn);
                b1 = -2.0 * A * ((A - 1.0) + (A + 1.0) * cs);
                b2 = A * ((A + 1.0) + (A - 1.0) * cs - beta * sn);
                a0 = (A + 1.0) - (A - 1.0) * cs + beta * sn;
                a1 = 2.0 * ((A - 1.0) - (A + 1.0) * cs);
                a2 = (A + 1.0) - (A - 1.0) * cs - beta * sn;
                break;

            case FilterType.AllPass:
                b0 = 1.0 - alpha;
                b1 = -2.0 * cs;
                b2 = 1.0 + alpha;
                a0 = 1.0 + alpha;
                a1 = -2.0 * cs;
                a2 = 1.0 - alpha;
                break;

            default:
                throw new ArgumentException("Unknown filter type");
        }

        // Normalize by a0 (critical for stability)
        _b0 = (float)(b0 / a0);
        _b1 = (float)(b1 / a0);
        _b2 = (float)(b2 / a0);
        _a1 = (float)(a1 / a0);
        _a2 = (float)(a2 / a0);
    }

    /// <summary>
    /// Process a single sample through the filter.
    /// Direct Form I implementation.
    ///
    /// NUMERICAL PRECISION:
    /// Using float32 is acceptable for audio (24-bit = 144dB dynamic range).
    /// For extreme parameter changes, use double precision coefficient calculation.
    /// </summary>
    public float Process(float input)
    {
        // Direct Form I: y[n] = b0*x[n] + b1*x[n-1] + b2*x[n-2] - a1*y[n-1] - a2*y[n-2]
        float output = _b0 * input + _b1 * _x1 + _b2 * _x2 - _a1 * _y1 - _a2 * _y2;

        // Shift delay line
        _x2 = _x1;
        _x1 = input;
        _y2 = _y1;
        _y1 = output;

        return output;
    }

    /// <summary>
    /// Process a buffer of samples in-place
    /// </summary>
    public void ProcessBuffer(float[] buffer, int offset, int count)
    {
        for (int i = offset; i < offset + count; i++)
        {
            buffer[i] = Process(buffer[i]);
        }
    }

    /// <summary>
    /// Reset filter state (clear delay line)
    /// Use when stopping/starting audio to prevent clicks
    /// </summary>
    public void Reset()
    {
        _x1 = _x2 = 0f;
        _y1 = _y2 = 0f;
    }
}

/// <summary>
/// AVOIDING INSTABILITY:
///
/// 1. Always normalize coefficients by a0
/// 2. Clamp frequency away from Nyquist (< sampleRate/2.5)
/// 3. Clamp Q to reasonable range (0.1 to 20)
/// 4. Use double precision for coefficient calculation
/// 5. Reset state when changing parameters dramatically
/// 6. Check for NaN/Inf in output (denormal prevention)
///
/// COEFFICIENT SCALING:
/// Why divide by a0? The transfer function is:
/// H(z) = (b0 + b1*z^-1 + b2*z^-2) / (a0 + a1*z^-1 + a2*z^-2)
///
/// We can divide numerator and denominator by a0:
/// H(z) = (b0/a0 + b1/a0*z^-1 + b2/a0*z^-2) / (1 + a1/a0*z^-1 + a2/a0*z^-2)
///
/// This normalizes the denominator, ensuring numerical stability.
/// </summary>

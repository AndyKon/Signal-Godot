using System;
using System.Collections.Generic;

namespace Signal.Minigame;

// No Godot dependencies — pure math. Testable independently.

public enum SignalType
{
    CrewLog,       // Organic, irregular peaks — voice-like
    SensorData,    // Clean periodic sine wave — regular, mathematical
    SystemMessage, // Sharp square wave — digital, precise timing
    Encrypted      // Pseudo-random but with hidden repeating pattern
}

public class NoiseLayer
{
    public float Frequency;
    public float Amplitude;
    public float Phase;
    public SignalType? MimicType; // If set, this noise mimics a signal type (decoy)
}

public class WaveformData
{
    // Configuration
    public const int SampleCount = 512;
    // Maps [0,1] normalised frequency to DFT bin index.
    // 32 gives 4–10 visible cycles in the waveform and good spectral separation.
    private const double FrequencyScale = 32.0;

    // The "answer" — what the player is trying to find
    public SignalType CorrectType { get; }
    public float SignalFrequency { get; }
    public float SignalAmplitude { get; }
    public float SignalPhase { get; }

    // Current filter state (set by player via UI)
    public float FilterFrequency { get; set; }
    public float FilterBandwidth { get; set; }
    public float FilterAmplitude { get; set; }
    public float FilterPhase { get; set; }

    // Noise layers
    private readonly List<NoiseLayer> _noiseLayers;
    private readonly int _seed;

    // Cached pure signal for clarity comparison
    private readonly float[] _pureSignal;

    // --- Construction ---

    /// <param name="noiseProximity">0.0 = noise stays far from signal, 1.0 = noise can overlap signal band.</param>
    public WaveformData(SignalType type, int noiseLayers, float signalStrength, int seed,
                        float noiseProximity = 0.0f)
    {
        CorrectType = type;
        _seed = seed;

        var rng = new Random(seed);

        // Each signal type lives in a characteristic frequency range.
        // This gives the player a learnable pattern:
        //   CrewLog = low frequencies (left side of display)
        //   SensorData = mid frequencies (center)
        //   SystemMessage = high frequencies (right side)
        //   Encrypted = varies (harder to predict)
        // Small random offset within each range prevents memorization.
        SignalFrequency = type switch
        {
            SignalType.CrewLog => 0.15f + (float)rng.NextDouble() * 0.10f,       // [0.15, 0.25]
            SignalType.SensorData => 0.35f + (float)rng.NextDouble() * 0.10f,    // [0.35, 0.45]
            SignalType.SystemMessage => 0.55f + (float)rng.NextDouble() * 0.10f, // [0.55, 0.65]
            SignalType.Encrypted => 0.20f + (float)rng.NextDouble() * 0.40f,     // [0.20, 0.60] — unpredictable
            _ => 0.35f
        };
        SignalAmplitude = signalStrength;
        SignalPhase = (float)(rng.NextDouble() * 2.0 * Math.PI);

        // Generate noise layers, scaling amplitude relative to signal strength.
        // noiseProximity controls how close noise can get to the signal band.
        _noiseLayers = new List<NoiseLayer>();
        for (int i = 0; i < noiseLayers; i++)
        {
            _noiseLayers.Add(GenerateNoiseLayer(rng, SignalFrequency, signalStrength, noiseProximity));
        }

        // Initialise filter to neutral (centred, wide open)
        FilterFrequency = 0.5f;
        FilterBandwidth = 1.0f;
        FilterAmplitude = 0.0f;
        FilterPhase = 0.0f;

        // Cache the pure signal for clarity calculations
        _pureSignal = GenerateSignalSamples(CorrectType, SignalFrequency, SignalAmplitude, SignalPhase, SampleCount);
    }

    // --- Noise layer generation ---

    // Spectral half-width per signal type (in normalised [0,1] frequency space).
    // Noise must stay outside this radius to be separable by bandpass.
    private static float SpectralRadius(SignalType type) => type switch
    {
        SignalType.CrewLog       => 0.20f, // formants span ~0.8x of fundamental
        SignalType.SensorData    => 0.06f, // fundamental + one harmonic
        SignalType.SystemMessage => 0.15f, // odd harmonics up to 15th
        SignalType.Encrypted     => 0.08f, // carrier with AM sidebands
        _ => 0.10f
    };

    private NoiseLayer GenerateNoiseLayer(Random rng, float signalFrequency, float signalStrength,
                                         float noiseProximity)
    {
        float baseExclusion = SpectralRadius(CorrectType) + 0.04f;
        // noiseProximity shrinks the exclusion zone: at 1.0 noise can sit right on the signal
        float exclusion = baseExclusion * (1.0f - noiseProximity);

        // At high proximity, some noise sits at the exact signal frequency (in-band).
        // This can't be removed by bandpass — only phase alignment helps.
        bool isInBand = noiseProximity > 0.3f && rng.NextDouble() < noiseProximity * 0.5;

        float freq;
        bool isNearSignal;
        if (isInBand)
        {
            // Exact same frequency as the signal — pure phase interference
            freq = signalFrequency;
            isNearSignal = true;
        }
        else
        {
            // 40% near signal, 60% random
            isNearSignal = rng.NextDouble() < 0.4;
            if (isNearSignal)
            {
                float offset = exclusion + (float)rng.NextDouble() * 0.10f;
                if (rng.NextDouble() < 0.5) offset = -offset;
                freq = Math.Clamp(signalFrequency + offset, 0.02f, 0.98f);
            }
            else
            {
                do
                {
                    freq = (float)rng.NextDouble();
                } while (Math.Abs(freq - signalFrequency) < exclusion);
            }
        }

        // Noise amplitude scales with signal strength:
        //   - Near-signal noise is weaker (40-80% of signal) so bandpass can still help
        //   - Far noise can be stronger (60-120% of signal) — it's easily filtered out
        float amp;
        if (isNearSignal)
            amp = signalStrength * (0.4f + (float)rng.NextDouble() * 0.4f);
        else
            amp = signalStrength * (0.6f + (float)rng.NextDouble() * 0.6f);

        bool isDecoy = rng.NextDouble() < 0.3; // 30% chance of being a decoy

        return new NoiseLayer
        {
            Frequency = freq,
            Amplitude = amp,
            Phase = (float)(rng.NextDouble() * 2.0 * Math.PI),
            MimicType = isDecoy ? (SignalType)(rng.Next(4)) : null
        };
    }

    // --- Sample generation ---

    public float[] GetRawSamples()
    {
        var samples = new float[SampleCount];

        // Add the hidden signal
        float[] signal = _pureSignal;
        for (int i = 0; i < SampleCount; i++)
            samples[i] = signal[i];

        // Add each noise layer
        foreach (var noise in _noiseLayers)
        {
            float[] noiseSamples;
            if (noise.MimicType.HasValue)
            {
                // Decoy: use that signal type's shape at the noise's frequency
                noiseSamples = GenerateSignalSamples(
                    noise.MimicType.Value, noise.Frequency, noise.Amplitude, noise.Phase, SampleCount);
            }
            else
            {
                // Plain sine noise
                noiseSamples = GenerateSineWave(noise.Frequency, noise.Amplitude, noise.Phase, SampleCount);
            }

            for (int i = 0; i < SampleCount; i++)
                samples[i] += noiseSamples[i];
        }

        return samples;
    }

    /// <summary>
    /// Returns samples that visually blend from raw (noisy) to pure signal
    /// based on the current clarity. This gives intuitive visual feedback —
    /// as the player tunes better, the waveform morphs into the clean signal.
    /// </summary>
    public float[] GetFilteredSamples()
    {
        float clarity = GetClarityInternal();
        float[] raw = GetRawSamples();
        float[] filtered = new float[SampleCount];

        // Blend: at clarity 0 → show raw noise. At clarity 1 → show pure signal.
        // Use a smoothed curve so the transition feels natural.
        float blend = clarity * clarity; // Quadratic ease — accelerates as you get closer
        for (int i = 0; i < SampleCount; i++)
        {
            filtered[i] = raw[i] * (1f - blend) + _pureSignal[i] * blend;
        }

        return filtered;
    }

    /// <summary>
    /// Returns the pure signal samples (no noise). Used by the display
    /// to show a dim reference line the player is trying to match.
    /// </summary>
    public float[] GetPureSignal()
    {
        return (float[])_pureSignal.Clone();
    }

    // --- Clarity ---

    /// <summary>
    /// Calculates how close the player's filter settings are to the ideal.
    /// Based on frequency proximity, bandwidth match, and phase alignment.
    /// Fast — no DFT, just parameter distance.
    /// </summary>
    public float GetClarity()
    {
        return GetClarityInternal();
    }

    private float GetClarityInternal()
    {
        // Frequency match: tight Gaussian. Sigma = 0.015.
        // At exact match: 1.0. Off by 0.015: 0.60. Off by 0.03: 0.13. Off by 0.05: 0.01.
        // This makes frequency tuning feel precise and responsive.
        float freqDist = Math.Abs(FilterFrequency - SignalFrequency);
        float freqMatch = (float)Math.Exp(-freqDist * freqDist / (2.0 * 0.015 * 0.015));

        // Phase match: normalized to [0,1] where 0 = worst, 1 = perfect alignment.
        // The signal phase is in radians [0, 2π], FilterPhase is in [0, 1].
        float normalizedSignalPhase = (float)(SignalPhase / (2.0 * Math.PI));
        float phaseDist = Math.Abs(FilterPhase - normalizedSignalPhase);
        if (phaseDist > 0.5f) phaseDist = 1f - phaseDist; // Wrap (phase is circular)
        // Tight Gaussian on phase too. Sigma = 0.08.
        // Perfect: 1.0. Off by 0.08: 0.60. Off by 0.15: 0.07.
        float phaseMatch = (float)Math.Exp(-phaseDist * phaseDist / (2.0 * 0.08 * 0.08));

        // Amplitude: acts as noise floor. Ideal value depends on noise level.
        // For simplicity: ideal is ~0.15 of signal amplitude. Generous tolerance.
        float idealAmp = SignalAmplitude * 0.15f;
        float ampDist = Math.Abs(FilterAmplitude - idealAmp);
        float ampMatch = (float)Math.Exp(-ampDist * ampDist / (2.0 * 0.1 * 0.1));

        // Combined: frequency 50%, phase 35%, amplitude 15%
        // At perfect settings: 1.0. Requires all three to be tuned for >0.95.
        float clarity = freqMatch * 0.50f + phaseMatch * 0.35f + ampMatch * 0.15f;
        return Math.Clamp(clarity, 0f, 1f);
    }

    public bool IsComplete(float threshold = 0.95f)
    {
        return GetClarity() >= threshold;
    }

    // --- Signal type generation (static, reusable) ---

    public static float[] GenerateSignalSamples(SignalType type, float frequency, float amplitude, float phase, int count)
    {
        return type switch
        {
            SignalType.CrewLog => GenerateCrewLog(frequency, amplitude, phase, count),
            SignalType.SensorData => GenerateSensorData(frequency, amplitude, phase, count),
            SignalType.SystemMessage => GenerateSystemMessage(frequency, amplitude, phase, count),
            SignalType.Encrypted => GenerateEncrypted(frequency, amplitude, phase, count),
            _ => throw new ArgumentOutOfRangeException(nameof(type))
        };
    }

    // --- CrewLog: sum of sine waves at close frequencies (voice formants) ---
    // Organic, irregular-looking peaks. The formant ratios are close to 1.0 so most
    // energy clusters near the fundamental — the filter can isolate them together.

    private static float[] GenerateCrewLog(float frequency, float amplitude, float phase, int count)
    {
        var samples = new float[count];
        double baseOmega = 2.0 * Math.PI * frequency * FrequencyScale;
        // Tight formant ratios: 1.0 to ~1.8 — keeps energy in a narrow band
        double[] freqRatios = { 1.0, 1.13, 1.31, 1.53, 1.79 };
        double[] amps = { 1.0, 0.7, 0.45, 0.3, 0.15 };

        double totalAmpWeight = 0;
        for (int j = 0; j < freqRatios.Length; j++)
            totalAmpWeight += amps[j];

        for (int i = 0; i < count; i++)
        {
            double t = (double)i / count;
            double value = 0.0;
            for (int j = 0; j < freqRatios.Length; j++)
            {
                value += amps[j] * Math.Sin(baseOmega * freqRatios[j] * t + phase + j * 0.7);
            }
            samples[i] = (float)(amplitude * value / totalAmpWeight);
        }

        return samples;
    }

    // --- SensorData: clean sine, plus a subtle second harmonic ---

    private static float[] GenerateSensorData(float frequency, float amplitude, float phase, int count)
    {
        var samples = new float[count];
        double omega = 2.0 * Math.PI * frequency * FrequencyScale;

        for (int i = 0; i < count; i++)
        {
            double t = (double)i / count;
            double value = 0.85 * Math.Sin(omega * t + phase)
                         + 0.15 * Math.Sin(2.0 * omega * t + phase);
            samples[i] = (float)(amplitude * value);
        }

        return samples;
    }

    // --- SystemMessage: square wave via sum of odd harmonics ---

    private static float[] GenerateSystemMessage(float frequency, float amplitude, float phase, int count)
    {
        var samples = new float[count];
        double omega = 2.0 * Math.PI * frequency * FrequencyScale;
        // Fourier series of square wave: sum of sin(n*x)/n for odd n
        const int harmonics = 15; // enough for sharp edges without Gibbs ringing

        for (int i = 0; i < count; i++)
        {
            double t = (double)i / count;
            double value = 0.0;
            for (int n = 1; n <= harmonics; n += 2)
            {
                value += Math.Sin(n * omega * t + phase) / n;
            }
            // Normalise: the Fourier square wave peaks at pi/4
            samples[i] = (float)(amplitude * value * (4.0 / Math.PI));
        }

        // Clamp to [-amplitude, amplitude] to enforce square shape
        for (int i = 0; i < count; i++)
            samples[i] = Math.Clamp(samples[i], -amplitude, amplitude);

        return samples;
    }

    // --- Encrypted: pseudo-random amplitude modulation on a carrier ---
    // Looks random but has a hidden repeating envelope AND a specific carrier frequency.
    // The carrier frequency places the energy where the bandpass filter can find it,
    // while the repeating envelope creates a subtle visual pattern.

    private static float[] GenerateEncrypted(float frequency, float amplitude, float phase, int count)
    {
        var samples = new float[count];
        double omega = 2.0 * Math.PI * frequency * FrequencyScale;

        // The hidden repeating period for the envelope
        int period = Math.Max(16, (int)(count * 0.15));

        // Generate one period of pseudo-random envelope values (seeded from phase)
        int seed = (int)(phase * 10000) ^ 0x5A5A;
        var rng = new Random(seed);
        var envelope = new float[period];
        for (int j = 0; j < period; j++)
        {
            envelope[j] = 0.4f + (float)rng.NextDouble() * 0.6f; // [0.4, 1.0]
        }

        // Smooth the envelope so it's not harsh step changes
        for (int pass = 0; pass < 3; pass++)
        {
            for (int j = 1; j < period - 1; j++)
            {
                envelope[j] = (envelope[j - 1] + envelope[j] * 2 + envelope[j + 1]) / 4f;
            }
        }

        // Modulate: carrier sine at the target frequency, shaped by the repeating envelope
        for (int i = 0; i < count; i++)
        {
            double t = (double)i / count;
            double carrier = Math.Sin(omega * t + phase);
            samples[i] = (float)(amplitude * carrier * envelope[i % period]);
        }

        return samples;
    }

    // --- Plain sine wave helper ---

    private static float[] GenerateSineWave(float frequency, float amplitude, float phase, int count)
    {
        var samples = new float[count];
        double omega = 2.0 * Math.PI * frequency * FrequencyScale;
        for (int i = 0; i < count; i++)
        {
            double t = (double)i / count;
            samples[i] = (float)(amplitude * Math.Sin(omega * t + phase));
        }
        return samples;
    }

    // --- DFT / Inverse DFT ---
    // Using direct DFT for correctness and simplicity at N=512.
    // For production at larger sizes, swap to FFT.

    private static void DFT(float[] input, double[] outReal, double[] outImag)
    {
        int n = input.Length;
        for (int k = 0; k < n; k++)
        {
            double sumR = 0, sumI = 0;
            for (int t = 0; t < n; t++)
            {
                double angle = 2.0 * Math.PI * k * t / n;
                sumR += input[t] * Math.Cos(angle);
                sumI -= input[t] * Math.Sin(angle);
            }
            outReal[k] = sumR;
            outImag[k] = sumI;
        }
    }

    private static void InverseDFT(double[] real, double[] imag, float[] output)
    {
        int n = output.Length;
        for (int t = 0; t < n; t++)
        {
            double sum = 0;
            for (int k = 0; k < n; k++)
            {
                double angle = 2.0 * Math.PI * k * t / n;
                sum += real[k] * Math.Cos(angle) - imag[k] * Math.Sin(angle);
            }
            output[t] = (float)(sum / n);
        }
    }

    // --- Normalized cross-correlation ---

    private static float NormalizedCrossCorrelation(float[] a, float[] b)
    {
        if (a.Length != b.Length || a.Length == 0)
            return 0f;

        int n = a.Length;

        // Compute means
        double meanA = 0, meanB = 0;
        for (int i = 0; i < n; i++)
        {
            meanA += a[i];
            meanB += b[i];
        }
        meanA /= n;
        meanB /= n;

        // Compute cross-correlation and standard deviations
        double crossSum = 0, varA = 0, varB = 0;
        for (int i = 0; i < n; i++)
        {
            double da = a[i] - meanA;
            double db = b[i] - meanB;
            crossSum += da * db;
            varA += da * da;
            varB += db * db;
        }

        double denom = Math.Sqrt(varA * varB);
        if (denom < 1e-12)
            return 0f;

        // Clamp to [0, 1] — negative correlation means anti-phase, which is still "wrong"
        float ncc = (float)(crossSum / denom);
        return Math.Clamp(ncc, 0f, 1f);
    }

    // --- Factory methods ---

    /// <summary>1 noise layer, strong signal, noise far from signal band.</summary>
    public static WaveformData CreateEasy(SignalType type, int seed)
    {
        return new WaveformData(type, noiseLayers: 1, signalStrength: 1.0f, seed, noiseProximity: 0.0f);
    }

    /// <summary>2 noise layers, moderate signal, noise starts to intrude.</summary>
    public static WaveformData CreateMedium(SignalType type, int seed)
    {
        return new WaveformData(type, noiseLayers: 2, signalStrength: 0.7f, seed, noiseProximity: 0.3f);
    }

    /// <summary>3-4 noise layers, weak signal, noise overlaps signal band.</summary>
    public static WaveformData CreateHard(SignalType type, int seed)
    {
        int layers = 3 + (seed & 1);
        return new WaveformData(type, noiseLayers: layers, signalStrength: 0.5f, seed, noiseProximity: 0.6f);
    }

    /// <summary>Shifting signal, camouflaged type — hardest puzzle.</summary>
    public static WaveformData CreateNereus(SignalType type, int seed)
    {
        // 4 noise layers with weak signal, noise fully intruding into signal band
        var data = new WaveformData(type, noiseLayers: 4, signalStrength: 0.35f, seed, noiseProximity: 0.85f);

        // Add an extra decoy layer that mimics the CORRECT type at a nearby frequency.
        // This overlaps spectrally with the real signal, so the player can't separate
        // them with bandpass alone — they must use phase alignment to distinguish them.
        var rng = new Random(seed ^ 0x4E5245);
        float decoyOffset = 0.03f + (float)rng.NextDouble() * 0.05f;
        if (rng.NextDouble() < 0.5) decoyOffset = -decoyOffset;

        data._noiseLayers.Add(new NoiseLayer
        {
            Frequency = Math.Clamp(data.SignalFrequency + decoyOffset, 0.05f, 0.60f),
            Amplitude = data.SignalAmplitude * 1.2f, // stronger than the real signal
            Phase = (float)(rng.NextDouble() * 2.0 * Math.PI),
            MimicType = type // same type as the real signal — a true camouflage
        });

        return data;
    }
}

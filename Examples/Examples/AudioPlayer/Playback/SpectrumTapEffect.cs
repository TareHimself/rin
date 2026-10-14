using Rin.Core.Audio.Effects;

namespace Examples.AudioPlayer.Playback;

/// <summary>
///     Passes audio through untouched and writes the level of each frequency band, 0 to 1, into the
///     buffer the parameters point at. The bands are spaced logarithmically from 40 Hz to 16 kHz.
/// </summary>
[AudioEffect]
public partial struct SpectrumTapEffect
{
    public const int WindowSize = 1024;
    private const int AnalysisInterval = WindowSize / 4;
    private const float LowestHz = 40f;
    private const float HighestHz = 16000f;
    private const float FloorDb = -60f;

    public struct Parameters
    {
        public IntPtr Bands;
        public int BandCount;
    }

    public struct State
    {
        public unsafe fixed float Window[WindowSize];
        public int Head;
        public int FramesSinceAnalysis;
    }

    public static void Process(
        in AudioEffectContext ctx,
        ReadOnlySpan<float> input,
        Span<float> output,
        in Parameters parameters,
        ref State state)
    {
        input.CopyTo(output);

        var channels = int.Max(ctx.Channels, 1);
        var frames = input.Length / channels;
        unsafe
        {
            for (var frame = 0; frame < frames; frame++)
            {
                var mono = 0f;
                for (var channel = 0; channel < channels; channel++) mono += input[frame * channels + channel];
                state.Window[state.Head] = mono / channels;
                state.Head = (state.Head + 1) % WindowSize;
            }
        }

        state.FramesSinceAnalysis += frames;
        if (parameters.Bands == IntPtr.Zero || parameters.BandCount <= 0) return;
        if (state.FramesSinceAnalysis < AnalysisInterval) return;

        state.FramesSinceAnalysis = 0;
        unsafe
        {
            Analyze(ref state, ctx.SampleRate, new Span<float>((float*)parameters.Bands, parameters.BandCount));
        }
    }

    private static void Analyze(ref State state, int sampleRate, Span<float> bands)
    {
        Span<float> real = stackalloc float[WindowSize];
        Span<float> imaginary = stackalloc float[WindowSize];
        CopyWindowed(ref state, real);
        imaginary.Clear();
        Fft(real, imaginary);

        var binHz = (float)sampleRate / WindowSize;
        var octaves = MathF.Log2(HighestHz / LowestHz);
        for (var band = 0; band < bands.Length; band++)
        {
            var fromHz = LowestHz * MathF.Pow(2f, octaves * band / bands.Length);
            var toHz = LowestHz * MathF.Pow(2f, octaves * (band + 1) / bands.Length);
            var fromBin = int.Clamp((int)(fromHz / binHz), 1, WindowSize / 2 - 1);
            var toBin = int.Clamp((int)MathF.Ceiling(toHz / binHz), fromBin + 1, WindowSize / 2);

            var peak = 0f;
            for (var bin = fromBin; bin < toBin; bin++)
            {
                var magnitude = MathF.Sqrt(real[bin] * real[bin] + imaginary[bin] * imaginary[bin]);
                peak = float.Max(peak, magnitude);
            }

            bands[band] = ToLevel(peak * 4f / WindowSize);
        }
    }

    private static unsafe void CopyWindowed(ref State state, Span<float> destination)
    {
        for (var i = 0; i < WindowSize; i++)
        {
            var hann = 0.5f - 0.5f * MathF.Cos(2f * MathF.PI * i / (WindowSize - 1));
            destination[i] = state.Window[(state.Head + i) % WindowSize] * hann;
        }
    }

    private static float ToLevel(float magnitude)
    {
        var db = 20f * MathF.Log10(float.Max(magnitude, 1e-6f));
        return float.Clamp((db - FloorDb) / -FloorDb, 0f, 1f);
    }

    private static void Fft(Span<float> real, Span<float> imaginary)
    {
        var n = real.Length;
        for (int i = 1, j = 0; i < n; i++)
        {
            var bit = n >> 1;
            for (; (j & bit) != 0; bit >>= 1) j ^= bit;
            j ^= bit;
            if (i >= j) continue;

            (real[i], real[j]) = (real[j], real[i]);
            (imaginary[i], imaginary[j]) = (imaginary[j], imaginary[i]);
        }

        for (var length = 2; length <= n; length <<= 1)
        {
            var angle = -2f * MathF.PI / length;
            var stepReal = MathF.Cos(angle);
            var stepImaginary = MathF.Sin(angle);
            for (var start = 0; start < n; start += length)
            {
                var twiddleReal = 1f;
                var twiddleImaginary = 0f;
                for (var k = 0; k < length / 2; k++)
                {
                    var even = start + k;
                    var odd = start + k + length / 2;
                    var productReal = real[odd] * twiddleReal - imaginary[odd] * twiddleImaginary;
                    var productImaginary = real[odd] * twiddleImaginary + imaginary[odd] * twiddleReal;
                    real[odd] = real[even] - productReal;
                    imaginary[odd] = imaginary[even] - productImaginary;
                    real[even] += productReal;
                    imaginary[even] += productImaginary;

                    var nextReal = twiddleReal * stepReal - twiddleImaginary * stepImaginary;
                    twiddleImaginary = twiddleReal * stepImaginary + twiddleImaginary * stepReal;
                    twiddleReal = nextReal;
                }
            }
        }
    }
}

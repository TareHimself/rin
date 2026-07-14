using System.Runtime.CompilerServices;
using Rin.Core.Audio.Effects;

namespace rin.Examples.ViewsTest;

// A bank of tuned resonators (think: a set of struck bells/strings) continuously excited by
// whatever's playing through it. Each resonator is a damped feedback delay tuned to a note of
// a just-intonation chord built above Parameters.RootHz, so the whole bank rings out as a
// consonant, bell/harp-like chord around the input rather than a literal echo. Every resonator
// drifts slightly out of tune against the others (ShimmerRateHz/ShimmerDepthCents) so the chord
// breathes instead of sounding static, and each is panned to its own spot in the stereo field.
[AudioEffect]
public partial struct BloomEffect
{
    private const int ResonatorCount = 6;
    private const int MaxDelaySamples = 8192;

    // Just-intonation ratios above the root: unison, major third, fifth, octave,
    // octave+third, octave+fifth - a two-octave major chord, chosen for its consonance
    // (just-intonation ratios beat far less than equal temperament, which is most of why
    // this sounds "beautiful" rather than merely "delayed").
    private static readonly float[] Ratios = [1f, 1.25f, 1.5f, 2f, 2.5f, 3f];

    public struct Parameters
    {
        public float RootHz = 220f;
        public float DecaySeconds = 3.5f;
        public float Damping = 0.35f;
        public float Spread = 0.5f;
        public float ShimmerRateHz = 0.15f;
        public float ShimmerDepthCents = 6f;
        public float Wet = 0.6f;
        public float Gain = 1f;

        public Parameters()
        {
        }
    }

    public struct State
    {
        // Per-resonator circular delay line, flattened: resonator `i`, sample `s` lives at
        // DelayBuffer[i * MaxDelaySamples + s].
        public unsafe fixed float DelayBuffer[ResonatorCount * MaxDelaySamples];
        public unsafe fixed int WriteIndex[ResonatorCount];

        // One-pole damping memory (rolls highs off each round trip, like a real string/bell).
        public unsafe fixed float DampMemory[ResonatorCount];

        // Per-resonator detune LFO phase in [0, 1), each independent so the chord breathes
        // instead of every voice wobbling in lockstep.
        public unsafe fixed double LfoPhase[ResonatorCount];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Process(
        in AudioEffectContext ctx,
        ReadOnlySpan<float> input,
        Span<float> output,
        in Parameters parameters,
        ref State state)
    {
        var sampleRate = MathF.Max(ctx.SampleRate, 1);
        var channels = Math.Max(ctx.Channels, 1);
        var frames = input.Length / channels;

        var root = Math.Clamp(parameters.RootHz, 40f, 2000f);
        var decay = Math.Clamp(parameters.DecaySeconds, 0.1f, 20f);
        var damping = Math.Clamp(parameters.Damping, 0f, 0.98f);
        var dampCoeff = 1f - damping;
        var spread = Math.Clamp(parameters.Spread, 0f, 1f);
        var shimmerRate = Math.Clamp(parameters.ShimmerRateHz, 0f, 5f);
        var shimmerCents = Math.Clamp(parameters.ShimmerDepthCents, 0f, 50f);
        var wet = Math.Clamp(parameters.Wet, 0f, 1f);
        var dry = 1f - wet;
        var gain = Math.Clamp(parameters.Gain, 0f, 4f);
        var wetNorm = 1f / MathF.Sqrt(ResonatorCount); // energy-normalize summing decorrelated voices

        Span<float> delaySamplesArr = stackalloc float[ResonatorCount];
        Span<float> feedbackGainArr = stackalloc float[ResonatorCount];
        Span<float> modDepthArr = stackalloc float[ResonatorCount];

        for (var i = 0; i < ResonatorCount; i++)
        {
            var freq = root * Ratios[i];
            var delaySamples = Math.Clamp(sampleRate / freq, 4f, MaxDelaySamples - 8f);
            delaySamplesArr[i] = delaySamples;

            // -60dB decay time: feedbackGain^(decaySeconds*sampleRate/delaySamples) = 0.001
            feedbackGainArr[i] = Math.Clamp(
                MathF.Exp(-6.907755f * delaySamples / (decay * sampleRate)), 0f, 0.999f);

            // Detune depth scales with the resonator's own tuning so every voice drifts by
            // the same musical amount (a fixed sample offset would detune high notes far more
            // than low ones).
            modDepthArr[i] = delaySamples * (MathF.Pow(2f, shimmerCents / 1200f) - 1f);
        }

        var lfoIncrement = shimmerRate / sampleRate;

        unsafe
        {
            for (var frame = 0; frame < frames; frame++)
            {
                var frameBase = frame * channels;

                var mono = 0f;
                for (var ch = 0; ch < channels; ch++) mono += input[frameBase + ch];
                mono /= channels;
                var excitation = mono * spread;

                var wetLeft = 0f;
                var wetRight = 0f;
                var wetMono = 0f;

                for (var i = 0; i < ResonatorCount; i++)
                {
                    var bufBase = i * MaxDelaySamples;
                    var writeIdx = state.WriteIndex[i];

                    state.LfoPhase[i] += lfoIncrement;
                    if (state.LfoPhase[i] >= 1.0) state.LfoPhase[i] -= 1.0;
                    var lfo = MathF.Sin((float)(state.LfoPhase[i] * (Math.PI * 2.0)));

                    var readPos = writeIdx - (delaySamplesArr[i] + modDepthArr[i] * lfo);
                    readPos %= MaxDelaySamples;
                    if (readPos < 0f) readPos += MaxDelaySamples;

                    var readIdx0 = (int)readPos;
                    var frac = readPos - readIdx0;
                    var readIdx1 = readIdx0 + 1 >= MaxDelaySamples ? 0 : readIdx0 + 1;

                    var delayed = state.DelayBuffer[bufBase + readIdx0] * (1f - frac) +
                                  state.DelayBuffer[bufBase + readIdx1] * frac;

                    // One-pole damping inside the loop: higher Damping = darker, warmer decay,
                    // like a real string/bell losing its upper harmonics fastest.
                    var resonated = dampCoeff * delayed + (1f - dampCoeff) * state.DampMemory[i];
                    state.DampMemory[i] = resonated;

                    var toWrite = Math.Clamp(excitation + resonated * feedbackGainArr[i], -4f, 4f);
                    state.DelayBuffer[bufBase + writeIdx] = toWrite;
                    state.WriteIndex[i] = writeIdx + 1 >= MaxDelaySamples ? 0 : writeIdx + 1;

                    // Equal-power pan, spread evenly across the stereo field by resonator index.
                    var panPos = ResonatorCount > 1 ? i / (float)(ResonatorCount - 1) * 2f - 1f : 0f;
                    var panAngle = (panPos + 1f) * 0.25f * MathF.PI;
                    wetLeft += resonated * MathF.Cos(panAngle);
                    wetRight += resonated * MathF.Sin(panAngle);
                    wetMono += resonated;
                }

                if (channels >= 2)
                {
                    output[frameBase] = dry * input[frameBase] + wet * wetLeft * wetNorm * gain;
                    output[frameBase + 1] = dry * input[frameBase + 1] + wet * wetRight * wetNorm * gain;
                    for (var ch = 2; ch < channels; ch++)
                        output[frameBase + ch] = dry * input[frameBase + ch] + wet * wetMono * wetNorm * gain;
                }
                else
                {
                    output[frameBase] = dry * input[frameBase] + wet * wetMono * wetNorm * gain;
                }
            }
        }
    }
}

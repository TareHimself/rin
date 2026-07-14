using System.Runtime.CompilerServices;
using Rin.Core.Audio.Effects;

namespace rin.Examples.ViewsTest;

// Deliberately pushes past what the other example effects exercise, to stress-test the
// [AudioEffect] pipeline itself rather than just to sound interesting:
//
//  - State here is two orders of magnitude larger than the ~8-float buffers the other
//    example effects use (a per-channel circular delay line), to exercise CreateState()'s
//    unmanaged allocation/marshaling for a large block rather than a handful of scalars.
//  - Fractional (linearly-interpolated) circular-buffer reads, which none of the other
//    effects need.
//  - A per-sample LFO phase accumulated in State rather than derived from ctx.Time: ctx.Time
//    only advances once per processed block, so driving audio-rate modulation from it directly
//    would zipper at block boundaries.
//  - Channel handling that isn't hardcoded to mono/stereo (unlike OrbitEffect/BassSpiceEffect's
//    stereo-only widening) - anything beyond MaxChannels is passed through untouched instead of
//    indexing an unsafe fixed buffer out of bounds.
//  - A feedback loop (resonant filter + saturation) that has to stay numerically stable under
//    live parameter changes, including parameter values well outside sane ranges.
[AudioEffect]
public partial struct StressTestEffect
{
    private const int MaxChannels = 8;
    private const int MaxDelaySamples = 8192; // ~170ms per channel @ 48kHz

    public struct Parameters
    {
        public float DelayMs = 220f;
        public float Feedback = 0.45f;
        public float ModRateHz = 0.35f;
        public float ModDepthMs = 6f;
        public float FilterCutoffHz = 3000f;
        public float FilterResonance = 0.25f;
        public float Drive = 1.6f;
        public float CrossFeed = 0.35f;
        public float Wet = 0.5f;
        public float Gain = 1f;

        public Parameters()
        {
        }
    }

    public struct State
    {
        // Per-channel circular delay line, flattened into a single fixed buffer:
        // channel `ch`, sample `i` lives at DelayBuffer[ch * MaxDelaySamples + i].
        public unsafe fixed float DelayBuffer[MaxChannels * MaxDelaySamples];
        public unsafe fixed int WriteIndex[MaxChannels];

        // Per-channel resonant (Chamberlin state-variable) filter memory for the feedback path.
        public unsafe fixed float FilterLow[MaxChannels];
        public unsafe fixed float FilterBand[MaxChannels];

        // Per-sample-accurate LFO phase in [0, 1); advanced once per sample rather than derived
        // from ctx.Time, which only updates once per processed block.
        public double LfoPhase;

        // Proof that State persists across calls for as long as the effect stays attached.
        public ulong ProcessCallCount;
        public ulong TotalFramesProcessed;
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
        var totalChannels = Math.Max(ctx.Channels, 1);
        var processedChannels = Math.Min(totalChannels, MaxChannels);
        var frames = input.Length / totalChannels;

        var modRate = Math.Clamp(parameters.ModRateHz, 0f, 20f);
        var modDepthSamples = Math.Clamp(parameters.ModDepthMs, 0f, 40f) * sampleRate / 1000f;
        var baseDelaySamples = Math.Clamp(parameters.DelayMs * sampleRate / 1000f, 1f, MaxDelaySamples - 4f);
        var feedback = Math.Clamp(parameters.Feedback, 0f, 0.97f);
        var drive = Math.Clamp(parameters.Drive, 0.1f, 8f);
        var crossFeed = Math.Clamp(parameters.CrossFeed, 0f, 1f);
        var wet = Math.Clamp(parameters.Wet, 0f, 1f);
        var dry = 1f - wet;
        var gain = Math.Clamp(parameters.Gain, 0f, 4f);

        var cutoff = Math.Clamp(parameters.FilterCutoffHz, 60f, sampleRate * 0.45f);
        var resonance = Math.Clamp(parameters.FilterResonance, 0f, 0.95f);
        var f = 2f * MathF.Sin(MathF.PI * cutoff / sampleRate);
        var q = 2f * (1f - resonance);

        var lfoIncrement = modRate / sampleRate;

        state.ProcessCallCount++;
        state.TotalFramesProcessed += (ulong)frames;

        unsafe
        {
            for (var frame = 0; frame < frames; frame++)
            {
                var frameBase = frame * totalChannels;

                state.LfoPhase += lfoIncrement;
                if (state.LfoPhase >= 1.0) state.LfoPhase -= 1.0;
                var lfo = MathF.Sin((float)(state.LfoPhase * (Math.PI * 2.0)));
                var modulatedDelay = modDepthSamples * lfo;

                var prevDelayed = 0f;

                for (var ch = 0; ch < totalChannels; ch++)
                {
                    var idx = frameBase + ch;
                    var x = input[idx];

                    if (ch >= processedChannels)
                    {
                        // Past what the fixed-size per-channel buffers were sized for:
                        // pass through untouched rather than index out of bounds.
                        output[idx] = x;
                        continue;
                    }

                    var bufBase = ch * MaxDelaySamples;
                    var writeIdx = state.WriteIndex[ch];

                    var readPos = writeIdx - (baseDelaySamples + modulatedDelay);
                    readPos %= MaxDelaySamples;
                    if (readPos < 0f) readPos += MaxDelaySamples;

                    var readIdx0 = (int)readPos;
                    var frac = readPos - readIdx0;
                    var readIdx1 = readIdx0 + 1 >= MaxDelaySamples ? 0 : readIdx0 + 1;

                    var delayed = state.DelayBuffer[bufBase + readIdx0] * (1f - frac) +
                                  state.DelayBuffer[bufBase + readIdx1] * frac;

                    // Resonant low-pass (Chamberlin SVF) tone-shapes the feedback path.
                    var low = state.FilterLow[ch];
                    var band = state.FilterBand[ch];
                    var high = delayed - low - q * band;
                    band = Math.Clamp(f * high + band, -8f, 8f);
                    low = Math.Clamp(f * band + low, -8f, 8f);
                    state.FilterBand[ch] = band;
                    state.FilterLow[ch] = low;

                    var saturated = MathF.Tanh(low * drive) / MathF.Max(drive, 1f);

                    // Ping-pong: a slice of the previous channel's delayed tap feeds this one.
                    var crossed = ch > 0 ? crossFeed * prevDelayed : 0f;
                    var toWrite = Math.Clamp(x + saturated * feedback + crossed, -4f, 4f);

                    state.DelayBuffer[bufBase + writeIdx] = toWrite;
                    state.WriteIndex[ch] = writeIdx + 1 >= MaxDelaySamples ? 0 : writeIdx + 1;

                    output[idx] = dry * x + wet * delayed * gain;
                    prevDelayed = delayed;
                }
            }
        }
    }
}

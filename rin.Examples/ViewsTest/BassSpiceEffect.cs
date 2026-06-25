using System;
using System.Runtime.CompilerServices;
using Rin.Core;
using Rin.Core.Audio.Effects;

namespace rin.Examples.ViewsTest;

[AudioEffect]
public partial struct BassSpiceEffect
{
    public struct Parameters
    {
        // How much boosted bass to add back into the original signal.
        public float BassBoost = 1.5f;

        // Frequency below which audio is considered "bass".
        public float BassCutoffHz = 160.0f;

        // Adds saturation to the extracted bass.
        public float BassDrive = 2.0f;

        // Blends processed signal with original.
        // 0 = dry, 1 = fully processed.
        public float Wet = 1.0f;

        // Final output gain.
        public float Gain = 0.9f;

        // Stereo widening amount for non-bass content.
        // 0 = unchanged, 1 = wider.
        public float Width = 0.15f;

        public Parameters()
        {
        }
    }

    public struct State
    {
        // One-pole low-pass memory, per channel.
        public unsafe fixed float BassPrev[8];

        // One-pole high-pass helper memory, per channel.
        public unsafe fixed float DcPrevInput[8];
        public unsafe fixed float DcPrevOutput[8];

        // Debug/feature-test state.
        public ulong ProcessCallCount;
        public float LastBassCutoffHz;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Process(
        in AudioEffectContext ctx,
        ReadOnlySpan<float> input,
        Span<float> output,
        in Parameters parameters,
        ref State state)
    {
        var channels = ctx.Channels;
        var frames = input.Length / channels;

        var bassCutoff = Math.Clamp(parameters.BassCutoffHz, 40.0f, ctx.SampleRate * 0.25f);
        var bassBoost = Math.Clamp(parameters.BassBoost, 0.0f, 6.0f);
        var bassDrive = Math.Clamp(parameters.BassDrive, 0.0f, 10.0f);
        var wet = Math.Clamp(parameters.Wet, 0.0f, 1.0f);
        var dry = 1.0f - wet;
        var gain = Math.Clamp(parameters.Gain, 0.0f, 4.0f);
        var width = Math.Clamp(parameters.Width, 0.0f, 1.0f);

        // Bass extraction low-pass.
        var bassA = 1.0f - MathF.Exp(-2.0f * MathF.PI * bassCutoff / ctx.SampleRate);
        var bassB = 1.0f - bassA;

        // Tiny DC blocker to reduce saturation offset/rumble.
        const float dcR = 0.995f;

        state.ProcessCallCount++;
        state.LastBassCutoffHz = bassCutoff;
        
        unsafe
        {
            for (var frame = 0; frame < frames; frame++)
            {
                var frameBase = frame * channels;

                for (var ch = 0; ch < channels; ch++)
                {
                    var idx = frameBase + ch;
                    var x = input[idx];

                    // Extract bass with a simple one-pole low-pass.
                    var bass = bassA * x + bassB * state.BassPrev[ch];
                    state.BassPrev[ch] = bass;

                    // Saturate only the bass component.
                    var spicyBass = MathF.Tanh(bass * bassDrive);

                    // Add enhanced bass back into the original.
                    var processed = x + spicyBass * bassBoost;

                    // DC blocker:
                    // y[n] = x[n] - x[n-1] + R * y[n-1]
                    var dcBlocked = processed - state.DcPrevInput[ch] + dcR * state.DcPrevOutput[ch];
                    state.DcPrevInput[ch] = processed;
                    state.DcPrevOutput[ch] = dcBlocked;

                    output[idx] = (dry * x + wet * dcBlocked) * gain;
                }

                // Optional stereo widening for stereo buffers.
                // Keeps bass mostly centered because widening is applied after bass enhancement.
                if (channels == 2 && width > 0.0f)
                {
                    var leftIdx = frameBase;
                    var rightIdx = frameBase + 1;

                    var left = output[leftIdx];
                    var right = output[rightIdx];

                    var mid = (left + right) * 0.5f;
                    var side = (left - right) * 0.5f;

                    side *= 1.0f + width;

                    output[leftIdx] = mid + side;
                    output[rightIdx] = mid - side;
                }
            }
        }
    }
}
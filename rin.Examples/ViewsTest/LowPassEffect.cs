using System.Runtime.CompilerServices;
using Rin.Core;
using Rin.Core.Audio.Effects;

namespace rin.Examples.ViewsTest;

[AudioEffect]
public partial struct LowPassEffect
{
    public struct Parameters()
    {
        public float CutoffHz = 600f;
        public float Wet = 1f;
        public float Gain = 1f;
    }

    public struct State
    {
        public unsafe fixed float Prev[8];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Process(
        in AudioEffectContext ctx,
        ReadOnlySpan<float> input,
        Span<float> output,
        in Parameters parameters,
        ref State state)
    {
        Console.WriteLine(parameters.CutoffHz);
        var channels = ctx.Channels;
        var frames = input.Length / channels;

        var cutoff = Math.Clamp(parameters.CutoffHz, 20f, ctx.SampleRate * 0.45f);
        var wet = Math.Clamp(parameters.Wet, 0f, 1f);
        var dry = 1f - wet;
        var gain = parameters.Gain;

        var a = 1f - MathF.Exp(-2f * MathF.PI * cutoff / ctx.SampleRate);
        var b = 1f - a;

        unsafe
        {
            for (var frame = 0; frame < frames; frame++)
            {
                for (var ch = 0; ch < channels; ch++)
                {
                    var idx = frame * channels + ch;

                    var x = input[idx];
                    var y = a * x + b * state.Prev[ch];

                    state.Prev[ch] = y;
                    output[idx] = ((dry * x) + (wet * y)) * gain;
                }
            }
        }
    }
}
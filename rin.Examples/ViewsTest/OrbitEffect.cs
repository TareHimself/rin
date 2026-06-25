using System.Runtime.CompilerServices;
using Rin.Core;
using Rin.Core.Audio.Effects;

namespace rin.Examples.ViewsTest;

[AudioEffect]
public partial struct OrbitEffect
{
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Process(in AudioEffectContext ctx, ReadOnlySpan<float> input, Span<float> output)
    {
        var angle = ctx.Time * MathF.PI * 2f; // full rotation every 1 second, slow down with * 0.5f etc

        var gainL = MathF.Cos(angle);
        var gainR = MathF.Sin(angle);

        for (var i = 0; i < input.Length; i += 2)
        {
            var mono    = (input[i] + input[i + 1]) * 0.25f;
            output[i]   = mono * gainL;
            output[i+1] = mono * gainR;
        }
    }
}
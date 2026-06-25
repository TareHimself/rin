using System.Numerics;
using System.Runtime.CompilerServices;
using Rin.Core;

namespace rin.Examples.ViewsTest;

[AudioEffect]
public partial struct WarmthEffect
{
    private const float Drive = 1.5f; // gentle push, try 1.0–3.0

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Process(ReadOnlySpan<float> input, Span<float> output)
    {
        var length = input.Length;
        var vSize  = Vector<float>.Count;
        var vDrive = new Vector<float>(Drive);
        var vOne   = Vector<float>.One;

        var i = 0;

        for (; i <= length - vSize; i += vSize)
        {
            var v = new Vector<float>(input.Slice(i, vSize)) * vDrive;
            // algebraic sigmoid: x / (1 + |x|)  →  always in (-1, 1)
            v = v / (vOne + Vector.Abs(v));
            v.CopyTo(output.Slice(i, vSize));
        }

        for (; i < length; i++)
        {
            var v = input[i] * Drive;
            output[i] = v / (1f + MathF.Abs(v));
        }
    }
}
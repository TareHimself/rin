namespace Rin.Core.Audio.Effects;

[AudioEffect]
public partial struct TestEffect
{
    // public static void Process(in AudioEffectContext ctx, ReadOnlySpan<float> input, Span<float> output)
    // {
    //     for (var i = 0; i < output.Length; i++)
    //     {
    //         output[i] = input[i];
    //     }
    // }
    
    
    public static void Process(ReadOnlySpan<float> input, Span<float> output)
    {
        for (var i = 0; i < input.Length; i++)
        {
            output[i] = input[i] * 0.2f;
        }
    }
}
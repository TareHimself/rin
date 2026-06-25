namespace Rin.Core.Audio.Effects;

public interface IAudioEffect : IAudioEffectBase<NoAudioEffectParameters>
{
    public static abstract void Process(in AudioEffectContext ctx, ReadOnlySpan<float> input, Span<float> output);
}
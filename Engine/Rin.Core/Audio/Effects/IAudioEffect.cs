namespace Rin.Core.Audio.Effects;

/// <remarks>
/// Not consumed by the <c>[AudioEffect]</c> source generator or the Miniaudio backend - implementing
/// this does nothing on its own, and the generator does not require or check for it. To define an
/// effect, write a <c>partial</c> struct/class annotated <c>[AudioEffect]</c> with a static
/// <c>Process</c> method whose parameters are recognized by name (<c>input</c>, <c>output</c>, and
/// optionally <c>ctx</c>/<c>parameters</c>/<c>state</c>) - see <see cref="global::Rin.Core.Audio.Effects.AudioEffectAttribute"/>
/// for the full contract. Kept for now as a documentation marker only.
/// </remarks>
public interface IAudioEffect : IAudioEffectBase<NoAudioEffectParameters>
{
    public static abstract void Process(in AudioEffectContext ctx, ReadOnlySpan<float> input, Span<float> output);
}
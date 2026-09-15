namespace Rin.Core.Audio.Effects;

/// <remarks>
/// Not consumed by the <c>[AudioEffect]</c> source generator or the Miniaudio backend - implementing
/// this does nothing on its own. An effect is defined purely by convention: a <c>partial</c>
/// struct/class annotated <c>[AudioEffect]</c> with a matching static <c>Process</c> method (see
/// <see cref="global::Rin.Core.Audio.Effects.AudioEffectAttribute"/>). Kept for now as a documentation marker only.
/// </remarks>
public interface IAudioEffectBase<TParams> where TParams : unmanaged
{

}
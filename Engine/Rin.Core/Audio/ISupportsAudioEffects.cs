using Rin.Core.Audio.Effects;

namespace Rin.Core.Audio;

/// <summary>
/// Implemented by anything that can host a chain of DSP effects - currently audio buses
/// (mixers/groups). Effects run in insertion order on every processed block, each getting a
/// chance to read the prior effect's output and write its own before the block continues downstream.
/// </summary>
public interface ISupportsAudioEffects
{
    /// <summary>
    /// Attaches an effect described by <paramref name="descriptor"/> and returns a controller for
    /// updating its parameters or removing the effect later. In practice you pass
    /// <c>YourEffectType.Descriptor</c>. Never implement <see cref="IAudioEffectDescriptor{TParams}"/> by hand -
    /// it's emitted for you by the <c>[AudioEffect]</c> source generator; see
    /// <see cref="global::Rin.Core.Audio.Effects.AudioEffectAttribute"/>.
    /// </summary>
    public IEffectController<TParams> AddEffect<TParams>(IAudioEffectDescriptor<TParams> descriptor) where TParams : unmanaged;

    /// <summary>
    /// Detaches and releases a previously added effect by its <see cref="IEffectController.Id"/>.
    /// Prefer disposing the <see cref="IEffectController"/> returned by <see cref="AddEffect{TParams}"/>
    /// instead of calling this directly.
    /// </summary>
    public void RemoveEffect(ulong effectId);
}
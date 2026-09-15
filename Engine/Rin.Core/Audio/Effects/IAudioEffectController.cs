namespace Rin.Core.Audio.Effects;

/// <remarks>
/// Not wired into <see cref="global::Rin.Core.Audio.ISupportsAudioEffects.AddEffect{TParams}"/> - that returns
/// an <see cref="IEffectController{TParams}"/> instead, which exposes <c>Parameters</c> as a settable
/// property rather than an <c>UpdateParameters</c> method. Kept for now as a documentation marker only.
/// </remarks>
public interface IAudioEffectController<TParams> where TParams : unmanaged
{
    public void UpdateParameters(in TParams parameters);
}
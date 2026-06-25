using Rin.Core.Audio.Effects;

namespace Rin.Core.Audio;

public interface ISupportsAudioEffects
{
    public IEffectController<TParams> AddEffect<TParams>(IAudioEffectDescriptor<TParams> descriptor) where TParams : unmanaged;
    public void RemoveEffect(ulong effectId);
}
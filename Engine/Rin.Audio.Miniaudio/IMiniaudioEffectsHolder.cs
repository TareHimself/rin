using Rin.Core.Audio;

namespace Rin.Audio.Miniaudio;

public interface IMiniaudioEffectsHolder : ISupportsAudioEffects
{
    public void OnEffectRemoved(ulong effectId);
}
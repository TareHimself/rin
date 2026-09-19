using Rin.Core.Audio;
using Rin.Core.Audio.Effects;

namespace Rin.Audio.Null;

internal sealed class NullAudioGroup : IAudioGroup
{
    private ulong _nextEffectId = 1;

    public float Volume { get; set; } = 1.0f;

    public IChannel Play(IAudioSample sample)
    {
        return new NullChannel();
    }

    public IPushStream CreatePushStream(int frequency, int channels)
    {
        return new NullPushStream();
    }

    public IAudioGroup CreateSubGroup()
    {
        return new NullAudioGroup();
    }

    public IEffectController<TParams> AddEffect<TParams>(IAudioEffectDescriptor<TParams> descriptor)
        where TParams : unmanaged
    {
        return new NullEffectController<TParams>(_nextEffectId++, default);
    }

    public void RemoveEffect(ulong effectId)
    {
    }

    public void Dispose()
    {
    }
}

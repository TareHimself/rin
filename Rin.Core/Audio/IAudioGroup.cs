using Rin.Core.Audio.Effects;

namespace Rin.Core.Audio;

public interface IAudioGroup : IDisposable, ISupportsAudioEffects
{
    float Volume { get; set; }
    IChannel Play(IAudioSample sample);
    IPushStream CreatePushStream(int frequency, int channels);
    IAudioGroup CreateSubGroup();
}

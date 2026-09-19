using Rin.Core.Audio;

namespace Rin.Audio.Null;

internal sealed class NullAudioSample : IAudioSample
{
    public IActiveAudio MakeActive()
    {
        return new NullActiveAudio();
    }

    public void Dispose()
    {
    }
}

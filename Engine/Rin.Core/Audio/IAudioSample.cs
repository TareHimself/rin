namespace Rin.Core.Audio;

public interface IAudioSample : IDisposable
{
    public IActiveAudio MakeActive();
}
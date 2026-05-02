namespace Rin.Framework.Audio;

public interface IAudioSample : IDisposable
{
    public IActiveAudio MakeActive();
}
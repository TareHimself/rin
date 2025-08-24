namespace Rin.Framework.Audio;

public interface ISample : IDisposable
{
    public IChannel ToChannel();
    public IChannel Play();
}
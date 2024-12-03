namespace rin.Framework.Audio;

public interface ISample : IDisposable
{
    public abstract IChannel ToChannel();
    public abstract IChannel Play();
}
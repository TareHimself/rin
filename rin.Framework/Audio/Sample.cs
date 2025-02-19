using ManagedBass;
using rin.Framework.Core;

namespace rin.Framework.Audio;

public class Sample : Disposable, ISample
{
    private readonly int _handle;

    protected Sample(int handle)
    {
        _handle = handle;
    }

    public IChannel ToChannel()
    {
        return new Channel(Bass.SampleGetChannel(_handle,
            BassFlags.SampleChannelStream | BassFlags.Decode | BassFlags.AsyncFile));
    }

    public IChannel Play()
    {
        var channel = ToChannel();
        channel.Play();
        return channel;
    }

    public static Sample FromFile(string filePath)
    {
        return new Sample(Bass.SampleLoad(filePath, 0, 0, 1000, 0));
    }

    protected override void OnDispose(bool isManual)
    {
        Bass.SampleFree(_handle);
    }
}
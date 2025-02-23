using ManagedBass;
using rin.Framework.Core;

namespace rin.Framework.Audio.BassAudio;

public class BassSample : ISample
{
    private readonly int _handle;

    public BassSample(int handle)
    {
        _handle = handle;
    }

    public IChannel ToChannel()
    {
        return new BassChannel(Bass.SampleGetChannel(_handle,
            BassFlags.SampleChannelStream | BassFlags.Decode | BassFlags.AsyncFile));
    }

    public IChannel Play()
    {
        var channel = ToChannel();
        channel.Play();
        return channel;
    }

    public static BassSample FromFile(string filePath)
    {
        return new BassSample(Bass.SampleLoad(filePath, 0, 0, 1000, 0));
    }

    public void Dispose()
    {
        Bass.SampleFree(_handle);
    }
}
namespace Rin.Framework.Audio.Bass;

public class BassFileStreamSample : IAudioSample
{
    private readonly string _filePath;

    public BassFileStreamSample(string filePath)
    {
        _filePath = filePath;
    }

    // public IChannel ToChannel()
    // {
    //     ManagedBass.Bass.CreateStream()
    //     return new BassChannel(ManagedBass.Bass.SampleGetChannel(_handle,
    //         BassFlags.SampleChannelStream | BassFlags.Decode | BassFlags.AsyncFile));
    // }

    // public IChannel Play()
    // {
    //     var channel = ToChannel();
    //     channel.Play();
    //     return channel;
    // }

    public void Dispose()
    {
    }

    public IActiveAudio MakeActive()
    {
        return BassStreamChannel.FromFile(_filePath);
    }

    // public static BassFileStreamSample FromFile(string filePath)
    // {
    //     return new BassFileStreamSample(ManagedBass.Bass.SampleLoad(filePath, 0, 0, 1000, 0));
    // }
}
using ManagedBass;

namespace Rin.Framework.Audio.Bass;

public class BassAudioSample : IAudioSample
{
    private readonly int _handle;

    public BassAudioSample(int handle)
    {
        _handle = handle;
    }

    public void Dispose()
    {
        ManagedBass.Bass.SampleFree(_handle);
    }

    public IActiveAudio MakeActive()
    {
        return new BassChannel(ManagedBass.Bass.SampleGetChannel(_handle,
            BassFlags.SampleChannelStream | BassFlags.Decode | BassFlags.AsyncFile));
    }

    public static BassAudioSample FromFile(string filePath)
    {
        return new BassAudioSample(ManagedBass.Bass.SampleLoad(filePath, 0, 0, 1000, 0));
    }
}
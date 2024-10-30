using ManagedBass;
using rin.Core;

namespace rin.Audio;

public class AudioSample(int handle) : Disposable
{
    private readonly int _handle = handle;

    public static AudioSample FromFile(string filePath)
    {
        return new AudioSample(Bass.SampleLoad(filePath, 0, 0, 1000, 0));
    }

    protected override void OnDispose(bool isManual)
    {
        Bass.SampleFree(_handle);
    }

    public Channel GetChannel(bool forceNew = false)
    {
        return new Channel(Bass.SampleGetChannel(_handle, forceNew));
    }
}
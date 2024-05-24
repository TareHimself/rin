using ManagedBass;

namespace aerox.Runtime.Audio;

public class Sample(int handle) : Disposable
{
    private readonly int _handle = handle;

    public static Sample FromFile(string filePath)
    {
        return new Sample(Bass.SampleLoad(filePath, 0, 0, 1000, 0));
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
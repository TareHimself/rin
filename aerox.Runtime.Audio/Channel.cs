using ManagedBass;

namespace aerox.Runtime.Audio;

public class Channel(int handle) : Disposable
{
    private readonly int _handle = handle;

    public bool Play(bool restart = false)
    {
        return Bass.ChannelPlay(_handle, restart);
    }

    public bool SetVolume(float value)
    {
        return Bass.ChannelSetAttribute(_handle, ChannelAttribute.Volume, value);
    }

    public bool SetPitch(float value)
    {
        return Bass.ChannelSetAttribute(_handle, ChannelAttribute.Pitch, value);
    }

    protected override void OnDispose(bool isManual)
    {
        Bass.StreamFree(_handle);
    }
}
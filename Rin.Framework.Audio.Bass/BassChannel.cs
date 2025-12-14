using ManagedBass;

namespace Rin.Framework.Audio.Bass;

public class BassChannel : IChannel
{
    protected readonly int _handle;

    private readonly Sync _onEndSync = new();

    public BassChannel(int handle)
    {
        _handle = handle;

        ManagedBass.Bass.ChannelSetSync(handle, SyncFlags.End, 0, _onEndSync);

        _onEndSync.OnSync += (_, __, ___, ____) => OnEnd?.Invoke();
    }

    public bool IsPlaying => ManagedBass.Bass.ChannelIsActive(_handle) == PlaybackState.Playing;
    public bool IsPaused => ManagedBass.Bass.ChannelIsActive(_handle) == PlaybackState.Paused;

    public double Position => ManagedBass.Bass.ChannelBytes2Seconds(_handle, ManagedBass.Bass.ChannelGetPosition(_handle));

    public double Duration => ManagedBass.Bass.ChannelBytes2Seconds(_handle, ManagedBass.Bass.ChannelGetLength(_handle));

    public bool Play(bool restart = false)
    {
        return ManagedBass.Bass.ChannelPlay(_handle, restart);
    }

    public bool Pause()
    {
        return ManagedBass.Bass.ChannelPause(_handle);
    }

    public bool SetVolume(float value)
    {
        return ManagedBass.Bass.ChannelSetAttribute(_handle, ChannelAttribute.Volume, value);
    }


    /// <summary>
    ///     Sets the position of this channel in seconds
    /// </summary>
    /// <param name="position">The new position</param>
    /// <returns></returns>
    public bool SetPosition(double position)
    {
        return ManagedBass.Bass.ChannelSetPosition(_handle, ManagedBass.Bass.ChannelSeconds2Bytes(_handle, position));
    }

    public void Dispose()
    {
        ManagedBass.Bass.StreamFree(_handle);
    }

    public event Action? OnEnd;

    public bool SetPitch(float value)
    {
        return ManagedBass.Bass.ChannelSetAttribute(_handle, ChannelAttribute.Pitch, value);
    }
}
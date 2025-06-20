using ManagedBass;

namespace Rin.Engine.Audio.BassAudio;

public class BassChannel : IChannel
{
    protected readonly int _handle;

    private readonly Sync _onEndSync = new();

    public BassChannel(int handle)
    {
        _handle = handle;

        Bass.ChannelSetSync(handle, SyncFlags.End, 0, _onEndSync);

        _onEndSync.OnSync += (_, __, ___, ____) => OnEnd?.Invoke();
    }

    public bool IsPlaying => Bass.ChannelIsActive(_handle) == PlaybackState.Playing;
    public bool IsPaused => Bass.ChannelIsActive(_handle) == PlaybackState.Paused;

    public double Position => Bass.ChannelBytes2Seconds(_handle, Bass.ChannelGetPosition(_handle));

    public double Length => Bass.ChannelBytes2Seconds(_handle, Bass.ChannelGetLength(_handle));

    public bool Play(bool restart = false)
    {
        return Bass.ChannelPlay(_handle, restart);
    }

    public bool Pause()
    {
        return Bass.ChannelPause(_handle);
    }

    public bool SetVolume(float value)
    {
        return Bass.ChannelSetAttribute(_handle, ChannelAttribute.Volume, value);
    }


    /// <summary>
    ///     Sets the position of this channel in seconds
    /// </summary>
    /// <param name="position">The new position</param>
    /// <returns></returns>
    public bool SetPosition(double position)
    {
        return Bass.ChannelSetPosition(_handle, Bass.ChannelSeconds2Bytes(_handle, position));
    }

    public void Dispose()
    {
        Bass.StreamFree(_handle);
    }

    public event Action? OnEnd;

    public bool SetPitch(float value)
    {
        return Bass.ChannelSetAttribute(_handle, ChannelAttribute.Pitch, value);
    }
}
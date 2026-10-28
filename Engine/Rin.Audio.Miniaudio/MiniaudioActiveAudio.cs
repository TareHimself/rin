using Rin.Core.Audio;

namespace Rin.Audio.Miniaudio;

public class MiniaudioActiveAudio : IActiveAudio, IChannel
{
    private bool _disposed;

    internal MiniaudioActiveAudio(IMiniaudioEngine engine, ulong id)
    {
        Engine = engine;
        Id = id;
    }

    private protected IMiniaudioEngine Engine { get; }

    protected ulong Id { get; }

    public bool IsPlaying
    {
        get
        {
            ThrowIfDisposed();
            return Engine.ActiveIsPlaying(Id);
        }
    }

    public bool HasEnded
    {
        get
        {
            ThrowIfDisposed();
            return Engine.ActiveHasEnded(Id);
        }
    }

    public double Position
    {
        get
        {
            ThrowIfDisposed();
            return Engine.ActiveGetPosition(Id);
        }
    }

    public double Duration
    {
        get
        {
            ThrowIfDisposed();
            return Engine.ActiveGetDuration(Id);
        }
    }

    public bool Play(bool restart = false)
    {
        ThrowIfDisposed();
        return Engine.ActivePlay(Id, restart);
    }

    public bool Pause()
    {
        ThrowIfDisposed();
        return Engine.ActivePause(Id);
    }

    public bool SetVolume(float value)
    {
        ThrowIfDisposed();
        return Engine.ActiveSetVolume(Id, value);
    }

    public bool SetPosition(double position)
    {
        ThrowIfDisposed();
        return Engine.ActiveSetPosition(Id, position);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        GC.SuppressFinalize(this);
        Engine.ActiveDispose(Id);
    }

    protected void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(_disposed, this);
}

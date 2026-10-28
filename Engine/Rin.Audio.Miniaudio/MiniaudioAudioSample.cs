using Rin.Core.Audio;

namespace Rin.Audio.Miniaudio;

public class MiniaudioAudioSample : IAudioSample
{
    private readonly IMiniaudioEngine _engine;
    private bool _disposed;

    internal MiniaudioAudioSample(IMiniaudioEngine engine, ulong id)
    {
        _engine = engine;
        NativeId = id;
    }

    internal ulong NativeId { get; }

    public IActiveAudio MakeActive()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return new MiniaudioActiveAudio(_engine, _engine.SampleMakeActive(NativeId));
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        GC.SuppressFinalize(this);
        _engine.SampleDispose(NativeId);
    }
}

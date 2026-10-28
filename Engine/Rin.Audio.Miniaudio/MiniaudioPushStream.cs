using Rin.Core.Audio;

namespace Rin.Audio.Miniaudio;

public class MiniaudioPushStream : MiniaudioActiveAudio, IPushStream
{
    internal MiniaudioPushStream(IMiniaudioEngine engine, ulong id) : base(engine, id)
    {
    }

    public void Push(in ReadOnlySpan<byte> data)
    {
        ThrowIfDisposed();
        Engine.PushStreamPush(Id, data);
    }
}

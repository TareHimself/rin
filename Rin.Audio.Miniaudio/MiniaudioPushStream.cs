using Rin.Core.Audio;

namespace Rin.Audio.Miniaudio;

public class MiniaudioPushStream : MiniaudioActiveAudio, IPushStream
{
    internal MiniaudioPushStream(ulong id) : base(id)
    {
    }

    public unsafe void Push(in ReadOnlySpan<byte> data)
    {
        fixed (byte* ptr = data)
            Native.audioPushStreamPush(Id, ptr, (nuint)data.Length);
    }
}

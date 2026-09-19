using Rin.Core.Audio;

namespace Rin.Audio.Null;

internal sealed class NullPushStream : NullActiveAudio, IPushStream
{
    public void Push(in ReadOnlySpan<byte> data)
    {
    }
}

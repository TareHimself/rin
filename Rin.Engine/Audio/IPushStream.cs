namespace Rin.Engine.Audio;

public interface IPushStream : IStream
{
    public void Push(ReadOnlySpan<byte> data);
}
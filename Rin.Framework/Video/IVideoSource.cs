namespace Rin.Framework.Video;

public interface IVideoSource : IDisposable
{
    public ulong Length { get; }
    public ulong Available { get; }
    public void Read(ulong offset, Span<byte> destination);
}
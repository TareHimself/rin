using Rin.Engine.Graphics;

namespace Rin.Engine.Video;


public interface IVideoSource: IDisposable
{
    public void Read(ulong offset, Span<byte> destination);
    public ulong Length { get; }
    public ulong Available { get; }
}
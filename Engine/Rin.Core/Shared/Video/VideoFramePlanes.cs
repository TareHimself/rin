using Rin.Core.Graphics;

namespace Rin.Core.Shared.Video;

/// <summary>
///     Where the two planes of an NV12 frame sit in the buffer <see cref="IVideoPlayer.CopyRecentFrame" /> fills:
///     the luma plane first, one byte per pixel, then the chroma plane, one Cb and Cr byte per 2x2 pixels.
/// </summary>
public readonly record struct VideoFramePlanes(Extent2D Extent, ulong LumaByteSize, Extent2D ChromaExtent, ulong ChromaByteSize)
{
    public ulong ByteSize => LumaByteSize + ChromaByteSize;
}

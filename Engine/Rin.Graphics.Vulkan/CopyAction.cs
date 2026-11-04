using Rin.Core.Graphics;

namespace Rin.Graphics.Vulkan;

/// <summary>
///     What a <see cref="SharedWrite" /> does when it runs: a copy from one resource into another.
/// </summary>
internal abstract class CopyAction(ResourceHandle source, ResourceHandle destination)
{
    public ResourceHandle Source { get; } = source;
    public ResourceHandle Destination { get; } = destination;
}

internal sealed class BufferCopy(
    ResourceHandle source,
    ResourceHandle destination,
    ulong sourceOffset,
    ulong destinationOffset,
    ulong size) : CopyAction(source, destination)
{
    public ulong SourceOffset { get; } = sourceOffset;
    public ulong DestinationOffset { get; } = destinationOffset;
    public ulong Size { get; } = size;
}

internal sealed class TextureCopy(
    ResourceHandle source,
    ResourceHandle destination,
    Offset2D sourceOffset,
    Offset2D destinationOffset,
    Extent2D extent) : CopyAction(source, destination)
{
    public Offset2D SourceOffset { get; } = sourceOffset;
    public Offset2D DestinationOffset { get; } = destinationOffset;
    public Extent2D Extent { get; } = extent;
}

internal sealed class BufferToTextureCopy(
    ResourceHandle source,
    ResourceHandle destination,
    ulong sourceOffset,
    ulong size,
    Offset2D destinationOffset,
    Extent2D extent) : CopyAction(source, destination)
{
    public ulong SourceOffset { get; } = sourceOffset;
    public ulong Size { get; } = size;
    public Offset2D DestinationOffset { get; } = destinationOffset;
    public Extent2D Extent { get; } = extent;
}

internal sealed class TextureToBufferCopy(
    ResourceHandle source,
    ResourceHandle destination,
    Offset2D sourceOffset,
    Extent2D extent,
    ulong destinationOffset,
    ulong size) : CopyAction(source, destination)
{
    public Offset2D SourceOffset { get; } = sourceOffset;
    public Extent2D Extent { get; } = extent;
    public ulong DestinationOffset { get; } = destinationOffset;
    public ulong Size { get; } = size;
}

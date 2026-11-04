using Rin.Core.Graphics;

namespace Rin.Graphics.Vulkan;

internal enum LaneKind
{
    Buffer,
    Texture
}

/// <summary>
///     Names one typed lane: which lane set holds it and the resource it belongs to.
/// </summary>
internal readonly record struct LaneRef(LaneKind Kind, ResourceHandle Handle)
{
    public static LaneRef For(in ResourceHandle handle)
    {
        return new LaneRef(handle.Type == ResourceType.Buffer ? LaneKind.Buffer : LaneKind.Texture, handle);
    }
}

/// <summary>
///     Work that touches a source and a destination lane. The same instance sits in the <see cref="SharedWriteLane" />
///     and, as a fence, in each lane it names (once when both are the same lane).
/// </summary>
internal sealed class SharedWrite(CopyAction action) : PendingResourceAction
{
    public LaneRef Source { get; } = LaneRef.For(action.Source);
    public LaneRef Destination { get; } = LaneRef.For(action.Destination);
    public CopyAction Action { get; } = action;

    public bool IsSingleLane => Source.Equals(Destination);

    public bool Taken { get; internal set; }
}

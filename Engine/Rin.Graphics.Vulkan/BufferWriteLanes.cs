namespace Rin.Graphics.Vulkan;

internal sealed class BufferWriteLanes : ResourceLanes<PendingBufferWrite>
{
    protected override bool CanMerge(PendingBufferWrite existing, PendingBufferWrite incoming)
    {
        return incoming.Offset <= existing.Offset && incoming.End >= existing.End;
    }
}

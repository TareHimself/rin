namespace Rin.Graphics.Vulkan;

internal sealed class TextureWriteLanes : ResourceLanes<PendingTextureWrite>
{
    protected override bool CanMerge(PendingTextureWrite existing, PendingTextureWrite incoming)
    {
        return incoming.CoversWholeImage;
    }

    protected override PendingTextureWrite Merge(PendingTextureWrite existing, PendingTextureWrite incoming)
    {
        incoming.AdoptTasks(existing);
        return incoming;
    }
}

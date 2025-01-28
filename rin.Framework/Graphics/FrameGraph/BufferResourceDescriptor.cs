namespace rin.Framework.Graphics.FrameGraph;

public class BufferResourceDescriptor(ulong size) : IResourceDescriptor
{
    public readonly ulong Size = size;

    public override int GetHashCode()
    {
        return Size.GetHashCode();
    }
}
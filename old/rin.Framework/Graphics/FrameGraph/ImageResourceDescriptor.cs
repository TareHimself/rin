using TerraFX.Interop.Vulkan;

namespace rin.Framework.Graphics.FrameGraph;

public struct ImageResourceDescriptor : IResourceDescriptor, IEquatable<ImageResourceDescriptor>
{

    public required uint Width;
    public required uint Height;
    public required ImageFormat Format;
    public required VkImageUsageFlags Flags;
    public required VkImageLayout InitialLayout;

    public bool Equals(ImageResourceDescriptor other)
    {
        return Width == other.Width && Height == other.Height && Format == other.Format && Flags == other.Flags && InitialLayout == other.InitialLayout;
    }

    public override bool Equals(object? obj)
    {
        return obj is ImageResourceDescriptor other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Width, Height, (int)Format, (int)Flags, (int)InitialLayout);
    }
}
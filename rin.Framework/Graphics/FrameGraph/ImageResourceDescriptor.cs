using TerraFX.Interop.Vulkan;

namespace rin.Framework.Graphics.FrameGraph;

public class ImageResourceDescriptor : IResourceDescriptor
{
    public readonly VkImageUsageFlags Flags;
    public readonly ImageFormat Format;
    public readonly uint Height;
    public readonly ImageLayout InitialLayout;

    public readonly uint Width;

    public ImageResourceDescriptor(uint width, uint height, ImageFormat format, VkImageUsageFlags flags,
        ImageLayout initialLayout)
    {
        Width = width;
        Height = height;
        Format = format;
        Flags = flags;
        InitialLayout = initialLayout;
    }

    // public bool Equals(ImageResourceDescriptor? other)
    // {
    //     return Width == other.Width && Height == other.Height && Format == other.Format && Flags == other.Flags && InitialLayout == other.InitialLayout;
    // }
    //
    // public override bool Equals(object? obj)
    // {
    //     return obj is ImageResourceDescriptor other && Equals(other);
    // }
    //
    // public override int GetHashCode()
    // {
    //     return HashCode.Combine(Width, Height, (int)Format, (int)Flags, (int)InitialLayout);
    //}
    public override int GetHashCode()
    {
        return HashCode.Combine(Width, Height, (int)Format, (int)Flags, (int)InitialLayout);
    }
}
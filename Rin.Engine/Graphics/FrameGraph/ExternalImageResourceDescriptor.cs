using JetBrains.Annotations;

namespace Rin.Engine.Graphics.FrameGraph;

public class ExternalImageResourceDescriptor : IResourceDescriptor
{
    [PublicAPI] public readonly IGraphImage Image;

    public ExternalImageResourceDescriptor(IGraphImage image)
    {
        Image = image;
    }

    public ExternalImageResourceDescriptor(IDeviceImage image, Action? onDispose = null)
    {
        Image = new ExternalImage(image, onDispose);
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
        return Image.GetHashCode();
    }
}
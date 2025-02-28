using TerraFX.Interop.Vulkan;

namespace Rin.Engine.Graphics.FrameGraph;

/// <summary>
///     Basic implementation of <see cref="IGraphImage" /> that is meant for external images,
///     <see cref="IGraphImage.Dispose" /> does not do anything
/// </summary>
public class ExternalImageProxy : IGraphImage
{
    public void Dispose()
    {
    }

    public required ImageFormat Format { get; set; }
    public required Extent3D Extent { get; set; }
    public required VkImage NativeImage { get; set; }
    public required VkImageView NativeView { get; set; }
    public required ImageLayout Layout { get; set; }
    public bool Owned => false;
}
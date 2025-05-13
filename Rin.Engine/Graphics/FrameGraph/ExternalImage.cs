using TerraFX.Interop.Vulkan;

namespace Rin.Engine.Graphics.FrameGraph;

/// <summary>
///     Basic implementation of <see cref="IGraphImage" /> that is meant for external images,
///     <see cref="IGraphImage.Dispose" /> does not do anything
/// </summary>
public class ExternalImage(IDeviceImage image, Action? onDispose = null) : IGraphImage
{
    public void Dispose()
    {
        onDispose?.Invoke();
    }

    public ImageFormat Format => image.Format;
    public Extent3D Extent => image.Extent;
    public VkImage NativeImage => image.NativeImage;
    public VkImageView NativeView => image.NativeView;
    public bool CreatedByGraph => false;
}
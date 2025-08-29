using Rin.Framework.Graphics.Textures;
using TerraFX.Interop.Vulkan;

namespace Rin.Framework.Graphics.FrameGraph;

/// <summary>
/// </summary>
public class ExternalImage(IImage2D image, Action? onDispose = null) : IGraphImage, IVulkanImage2D
{
    public bool CreatedByGraph => false;

    public void Dispose()
    {
        onDispose?.Invoke();
    }

    public ImageFormat Format => image.Format;
    public Extent3D Extent => image.Extent;
    public VkImage NativeImage => ((IVulkanImage2D)image).NativeImage;
    public VkImageView NativeView => ((IVulkanImage2D)image).NativeView;
    public ImageHandle BindlessHandle => ImageHandle.InvalidImage;
}
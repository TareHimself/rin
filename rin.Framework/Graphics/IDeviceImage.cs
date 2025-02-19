using rin.Framework.Graphics.FrameGraph;
using TerraFX.Interop.Vulkan;

namespace rin.Framework.Graphics;

public interface IDeviceImage : IGraphResource
{
    public ImageFormat Format { get; }
    public Extent3D Extent { get; }
    public VkImage NativeImage { get; }
    public VkImageView NativeView { get; set; }
}
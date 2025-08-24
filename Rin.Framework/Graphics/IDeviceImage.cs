using Rin.Framework.Graphics.FrameGraph;
using TerraFX.Interop.Vulkan;

namespace Rin.Framework.Graphics;

public interface IDeviceImage : IGraphResource
{
    public ImageFormat Format { get; }
    public Extent3D Extent { get; }
    public VkImage NativeImage { get; }
    public VkImageView NativeView { get; }
}
using rin.Framework.Graphics.FrameGraph;
using rin.Framework.Core;
using rin.Framework.Core.Math;
using TerraFX.Interop.Vulkan;
using TerraFX.Interop.Vulkan;
using static TerraFX.Interop.Vulkan.Vulkan;

namespace rin.Framework.Graphics;

public interface IDeviceImage :  IGraphResource
{
    public ImageFormat Format { get; }
    public VkExtent3D Extent { get; }
    public VkImage NativeImage { get; }
    public VkImageView NativeView { get; set; }
    
}
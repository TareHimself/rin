using Rin.Framework.Graphics.Graph;
using Rin.Framework.Graphics.Images;
using Rin.Framework.Graphics.Vulkan.Images;
using TerraFX.Interop.Vulkan;

namespace Rin.Framework.Graphics.Vulkan.Graph;

/// <summary>
/// </summary>
public class ExternalVulkanTextureArray(IVulkanTextureArray source, Action? onDispose = null) : IDisposableVulkanTextureArray
{
    public void Dispose()
    {
        onDispose?.Invoke();
    }

    public Extent2D Extent => source.Extent;
    public bool Mips => source.Mips;
    public ImageFormat Format => source.Format;
    public ImageHandle Handle => source.Handle;
    public VkImage VulkanImage => source.VulkanImage;
    public VkImageView VulkanView => source.VulkanView;
    public ImageLayout Layout { get => source.Layout; set  => source.Layout = value; }
    public IntPtr Allocation => source.Allocation;
    public uint Count =>  source.Count;
}
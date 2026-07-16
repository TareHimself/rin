using Rin.Core.Graphics;
using Rin.Graphics.Vulkan.Images;
using TerraFX.Interop.Vulkan;

namespace Rin.Graphics.Vulkan.Graph;

/// <summary>
/// </summary>
public class ExternalVulkanTexture(IVulkanTexture source, Action? onDispose = null) : IDisposableVulkanTexture
{
    public void Dispose()
    {
        onDispose?.Invoke();
    }

    public Extent2D Extent => source.Extent;
    public bool Mips => source.Mips;
    public ImageFormat Format => source.Format;
    public ResourceHandle Handle => source.Handle;
    public VkImage VulkanImage => source.VulkanImage;
    public VkImageView VulkanView => source.VulkanView;

    public ImageLayout Layout
    {
        get => source.Layout;
        set => source.Layout = value;
    }

    public IntPtr Allocation => source.Allocation;
}
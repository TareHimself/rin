using System.Diagnostics.CodeAnalysis;
using Rin.Framework.Graphics.Images;
using TerraFX.Interop.Vulkan;

namespace Rin.Framework.Graphics.Vulkan.Images;

public class BindlessTexture : BindlessResource,IDisposableVulkanTexture
{
    public IDisposableVulkanTexture? Source { get; set; }
    public Extent2D Extent => Source?.Extent ?? throw new NullReferenceException();
    public bool Mips  => Source?.Mips ?? throw new NullReferenceException();
    public ImageFormat Format  => Source?.Format ?? throw new NullReferenceException();
    public VkImage VulkanImage  => Source?.VulkanImage ?? throw new NullReferenceException();
    public VkImageView VulkanView  => Source?.VulkanView ?? throw new NullReferenceException();
    public ImageLayout Layout
    {
        get  => Source?.Layout ?? throw new NullReferenceException();
        set
        {
            if (Source is not null)
            {
                Source.Layout = value;
            }
            else
            {
                throw new NullReferenceException();
            }
        }
    }

    public void Dispose()
    {
        VulkanGraphicsModule.Get().GetBindlessImageFactory().FreeHandles(Handle);
    }
    
    public IntPtr Allocation => Source?.Allocation ?? throw new NullReferenceException();
}
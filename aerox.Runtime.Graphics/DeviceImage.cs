using TerraFX.Interop.Vulkan;

namespace aerox.Runtime.Graphics;

/// <summary>
///     GPU Image
/// </summary>
public class DeviceImage(
    VkImage inImage,
    VkImageView inView,
    VkExtent3D inExtent,
    EImageFormat inFormat,
    Allocator inAllocator,
    IntPtr inAllocation)
    : DeviceMemory(inAllocator, inAllocation)
{
    public readonly EImageFormat Format = inFormat;
    public VkExtent3D Extent = inExtent;
    public VkImage Image = inImage;
    public VkImageView View = inView;

    protected override void OnDispose(bool isManual)
    {
        Allocator.FreeImage(this);
    }


    public void Barrier(VkCommandBuffer cmd,VkImageLayout from,VkImageLayout to,ImageBarrierOptions? options = null)
    {
        SGraphicsModule.ImageBarrier(cmd,this,from,to,options);
    }
    
    public void CopyTo(VkCommandBuffer cmd,DeviceImage dest)
    {
        SGraphicsModule.CopyImageToImage(cmd,this,dest);
    }
}
using rin.Graphics.FrameGraph;
using TerraFX.Interop.Vulkan;

namespace rin.Graphics;

/// <summary>
///     GPU Image
/// </summary>
public class DeviceImage(
    VkImage inImage,
    VkImageView inView,
    VkExtent3D inExtent,
    ImageFormat inFormat,
    Allocator allocator,
    IntPtr allocation)
    : DeviceMemory(allocator, allocation), IGraphResource
{
    public readonly ImageFormat Format = inFormat;
    public VkExtent3D Extent = inExtent;
    public VkImage Image = inImage;
    public VkImageView View = inView;

    protected override void OnDispose(bool isManual)
    {
        Allocator.FreeImage(this);
    }


    public void Barrier(VkCommandBuffer cmd,VkImageLayout from,VkImageLayout to,ImageBarrierOptions? options = null)
    {
        cmd.ImageBarrier(this,from,to,options);
    }
    
    public void CopyTo(VkCommandBuffer cmd,DeviceImage dest)
    {
        cmd.CopyImageToImage(this,dest);
    }
    
    public void CopyTo(VkCommandBuffer cmd,DeviceImage dest,ImageFilter filter)
    {
        cmd.CopyImageToImage(this,dest,filter);
    }
    
    public void CopyTo(VkCommandBuffer cmd,VkImage dest,VkExtent3D destExtent,VkExtent3D? srcExtent = null)
    {
        cmd.CopyImageToImage(Image,dest,srcExtent.GetValueOrDefault(Extent),destExtent);
    }
}
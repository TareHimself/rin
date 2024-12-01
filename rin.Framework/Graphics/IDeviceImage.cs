using rin.Framework.Graphics.FrameGraph;
using rin.Framework.Core;
using TerraFX.Interop.Vulkan;

namespace rin.Framework.Graphics;

public interface IDeviceImage :  IGraphResource
{
    public ImageFormat Format { get; }
    public VkExtent3D Extent { get; }
    public VkImage NativeImage { get; }
    public VkImageView NativeView { get; set; }
    

    public void Barrier(VkCommandBuffer cmd,VkImageLayout from,VkImageLayout to,ImageBarrierOptions? options = null)
    {
        cmd.ImageBarrier(this,from,to,options);
    }
    
    public void CopyTo(VkCommandBuffer cmd,IDeviceImage dest)
    {
        cmd.CopyImageToImage(this,dest);
    }
    
    public void CopyTo(VkCommandBuffer cmd,IDeviceImage dest,ImageFilter filter)
    {
        cmd.CopyImageToImage(this,dest,filter);
    }
    
    public void CopyTo(VkCommandBuffer cmd,VkImage dest,VkExtent3D destExtent,VkExtent3D? srcExtent = null)
    {
        cmd.CopyImageToImage(NativeImage,dest,srcExtent.GetValueOrDefault(Extent),destExtent);
    }
}
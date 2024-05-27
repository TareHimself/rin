using TerraFX.Interop.Vulkan;

namespace aerox.Runtime.Graphics;

/// <summary>
///     GPU Image
/// </summary>
public class DeviceImage(
    VkImage inImage,
    VkImageView inView,
    VkExtent3D inExtent,
    VkFormat inFormat,
    Allocator inAllocator,
    IntPtr inAllocation)
    : DeviceMemory(inAllocator, inAllocation)
{
    public readonly VkFormat Format = inFormat;
    public VkExtent3D Extent = inExtent;
    public VkImage Image = inImage;
    public VkImageView View = inView;

    protected override void OnDispose(bool isManual)
    {
        Allocator.FreeImage(this);
    }
}
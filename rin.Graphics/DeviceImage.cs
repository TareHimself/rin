using rin.Graphics.FrameGraph;
using TerraFX.Interop.Vulkan;

namespace rin.Graphics;

/// <summary>
///     GPU Image
/// </summary>
public class DeviceImage : DeviceMemory, IDeviceImage
{
    /// <summary>
    ///     GPU Image
    /// </summary>
    public DeviceImage(VkImage inImage,
        VkImageView inView,
        VkExtent3D inExtent,
        ImageFormat inFormat,
        Allocator allocator,
        IntPtr allocation) : base(allocator, allocation)
    {
        NativeImage = inImage;
        NativeView = inView;
        Extent = inExtent;
        Format = inFormat;
    }

    protected override void OnDispose(bool isManual)
    {
        Allocator.FreeImage(this);
    }

    public ImageFormat Format { get; }

    public VkExtent3D Extent { get; }

    public VkImage NativeImage { get; }

    public VkImageView NativeView { get; set; }
}
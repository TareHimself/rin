using TerraFX.Interop.Vulkan;

namespace Rin.Engine.Graphics;

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
        Extent3D inExtent,
        ImageFormat inFormat,
        Allocator allocator,
        IntPtr allocation, string name) : base(allocator, allocation)
    {
        NativeImage = inImage;
        NativeView = inView;
        Extent = inExtent;
        Format = inFormat;
        Name = name;
    }

    public string Name { get; private set; }

    public ImageFormat Format { get; }

    public Extent3D Extent { get; }

    public VkImage NativeImage { get; }

    public VkImageView NativeView { get; set; }
    
    public override void Dispose()
    {
        Allocator.FreeImage(this);
    }
}
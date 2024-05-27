using TerraFX.Interop.Vulkan;
using static TerraFX.Interop.Vulkan.Vulkan;

namespace aerox.Runtime.Graphics;

public class Texture : MultiDisposable
{
    private readonly VkExtent3D _size;

    public readonly DeviceImage DeviceImage;

    public readonly ImageFilter Filter;
    public readonly ImageTiling Tiling;
    private ImageFormat _format;
    private bool _mipMapped;


    public Texture(byte[] data, VkExtent3D size, ImageFormat format, ImageFilter filter, ImageTiling tiling,
        bool mipMapped = true,
        string debugName = "Texture")
    {
        _size = size;
        _format = format;
        Filter = filter;
        Tiling = tiling;
        DeviceImage = null;
        _mipMapped = mipMapped;
        var subsystem = SRuntime.Get().GetModule<SGraphicsModule>();
        DeviceImage = subsystem.CreateImage(data, size, format,
            VkImageUsageFlags.VK_IMAGE_USAGE_SAMPLED_BIT,
            mipMapped, filter, debugName);
    }


    public DeviceImage GetDeviceImage()
    {
        return DeviceImage;
    }


    public void SetMipMapped(bool isMipMapped)
    {
        _mipMapped = isMipMapped;
    }

    public VkExtent3D GetSize()
    {
        return _size;
    }

    public async Task Save(string filePath)
    {
        var mod = SRuntime.Get().GetModule<SGraphicsModule>();
        var buffer = mod.GetAllocator().NewTransferBuffer(_size.depth * _size.width * _size.height * 4);
        await mod.ImmediateSubmitAsync(cmd =>
        {
            SGraphicsModule.ImageBarrier(cmd, DeviceImage, VkImageLayout.VK_IMAGE_LAYOUT_SHADER_READ_ONLY_OPTIMAL,
                VkImageLayout.VK_IMAGE_LAYOUT_TRANSFER_SRC_OPTIMAL);

            var copyRegion = new VkBufferImageCopy
            {
                bufferOffset = 0,
                bufferRowLength = 0,
                bufferImageHeight = 0,
                imageSubresource = new VkImageSubresourceLayers
                {
                    aspectMask = VkImageAspectFlags.VK_IMAGE_ASPECT_COLOR_BIT,
                    mipLevel = 0,
                    baseArrayLayer = 0,
                    layerCount = 1
                },
                imageExtent = _size
            };

            unsafe
            {
                vkCmdCopyImageToBuffer(cmd, DeviceImage.Image, VkImageLayout.VK_IMAGE_LAYOUT_TRANSFER_SRC_OPTIMAL,
                    buffer, 1, &copyRegion);
            }

            SGraphicsModule.ImageBarrier(cmd, DeviceImage, VkImageLayout.VK_IMAGE_LAYOUT_TRANSFER_SRC_OPTIMAL,
                VkImageLayout.VK_IMAGE_LAYOUT_SHADER_READ_ONLY_OPTIMAL);
        });

        // TODO read from buffer here
        buffer.Dispose();
    }

    protected override void OnDispose(bool isManual)
    {
        DeviceImage.Dispose();
    }
}
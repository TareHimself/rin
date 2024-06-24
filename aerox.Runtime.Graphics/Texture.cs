using TerraFX.Interop.Vulkan;
using static TerraFX.Interop.Vulkan.Vulkan;

namespace aerox.Runtime.Graphics;

public class Texture : MultiDisposable
{

    public readonly DeviceImage DeviceImage;

    public readonly EImageFilter Filter;
    public readonly EImageTiling Tiling;
    private EImageFormat Format => DeviceImage.Format;
    private bool MipMapped;

    public VkExtent3D Size => DeviceImage.Extent;


    public Texture(byte[] data, VkExtent3D size, EImageFormat format, EImageFilter filter, EImageTiling tiling,
        bool mipMapped = true,
        string debugName = "Texture")
    {
        Filter = filter;
        Tiling = tiling;
        MipMapped = mipMapped;
        var subsystem = SRuntime.Get().GetModule<SGraphicsModule>();
        DeviceImage = subsystem.CreateImage(data, size, format,
            VkImageUsageFlags.VK_IMAGE_USAGE_SAMPLED_BIT,
            mipMapped, filter, debugName);
    }


    public async Task Save(string filePath)
    {
        var mod = SRuntime.Get().GetModule<SGraphicsModule>();
        var buffer = mod.GetAllocator().NewTransferBuffer(Size.depth * Size.width * Size.height * 4);
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
                imageExtent = Size
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
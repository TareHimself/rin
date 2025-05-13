using TerraFX.Interop.Vulkan;

namespace Rin.Engine.Graphics;

/// <summary>
///     Options for image barriers
/// </summary>
public struct ImageBarrierOptions
{
    public VkAccessFlags2 SrcAccessFlags = VkAccessFlags2.VK_ACCESS_2_MEMORY_WRITE_BIT;

    public VkAccessFlags2 DstAccessFlags =
        VkAccessFlags2.VK_ACCESS_2_MEMORY_WRITE_BIT | VkAccessFlags2.VK_ACCESS_2_MEMORY_READ_BIT;

    public VkPipelineStageFlags2 WaitCompleteStages = VkPipelineStageFlags2.VK_PIPELINE_STAGE_2_ALL_COMMANDS_BIT;
    public VkPipelineStageFlags2 StartAfterStages = VkPipelineStageFlags2.VK_PIPELINE_STAGE_2_ALL_COMMANDS_BIT;

    public VkImageSubresourceRange SubresourceRange =
        SGraphicsModule.MakeImageSubresourceRange(VkImageAspectFlags.VK_IMAGE_ASPECT_COLOR_BIT);

    public ImageBarrierOptions()
    {
    }

    public ImageBarrierOptions(ImageFormat format, ImageLayout from, ImageLayout to)
    {
        switch (format)
        {
            case ImageFormat.R8:
            case ImageFormat.R16:
            case ImageFormat.R32:
            case ImageFormat.RG8:
            case ImageFormat.RG16:
            case ImageFormat.RG32:
            case ImageFormat.RGBA8:
            case ImageFormat.RGBA16:
            case ImageFormat.RGBA32:
            case ImageFormat.Swapchain:
                SubresourceRange =
                    SGraphicsModule.MakeImageSubresourceRange(VkImageAspectFlags.VK_IMAGE_ASPECT_COLOR_BIT);
                break;
            case ImageFormat.Depth:
                SubresourceRange =
                    SGraphicsModule.MakeImageSubresourceRange(VkImageAspectFlags.VK_IMAGE_ASPECT_DEPTH_BIT);
                break;
            case ImageFormat.Stencil:

                SubresourceRange = SGraphicsModule.MakeImageSubresourceRange(
                    VkImageAspectFlags.VK_IMAGE_ASPECT_STENCIL_BIT | VkImageAspectFlags.VK_IMAGE_ASPECT_DEPTH_BIT);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(format), format, null);
        }

        switch (from)
        {
            case ImageLayout.Undefined:
                // Assuming Nothing
                break;
            case ImageLayout.ColorAttachment:
                WaitCompleteStages = VkPipelineStageFlags2.VK_PIPELINE_STAGE_2_COLOR_ATTACHMENT_OUTPUT_BIT;
                break;
            case ImageLayout.StencilAttachment:
            case ImageLayout.DepthAttachment:
                WaitCompleteStages = VkPipelineStageFlags2.VK_PIPELINE_STAGE_2_LATE_FRAGMENT_TESTS_BIT;
                break;
            case ImageLayout.ShaderReadOnly:
                WaitCompleteStages = VkPipelineStageFlags2.VK_PIPELINE_STAGE_2_VERTEX_SHADER_BIT;
                break;
            case ImageLayout.General:
            case ImageLayout.TransferSrc:
            case ImageLayout.TransferDst:
                WaitCompleteStages = VkPipelineStageFlags2.VK_PIPELINE_STAGE_2_TRANSFER_BIT;
                break;
            case ImageLayout.PresentSrc:
                // Not Sure
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(from), from, null);
        }


        switch (to)
        {
            case ImageLayout.Undefined:
                // Assuming Nothing
                break;
            case ImageLayout.ColorAttachment:
                StartAfterStages = VkPipelineStageFlags2.VK_PIPELINE_STAGE_2_COLOR_ATTACHMENT_OUTPUT_BIT;
                break;
            case ImageLayout.StencilAttachment:
            case ImageLayout.DepthAttachment:
                StartAfterStages = VkPipelineStageFlags2.VK_PIPELINE_STAGE_2_EARLY_FRAGMENT_TESTS_BIT;
                break;
            case ImageLayout.ShaderReadOnly:
                StartAfterStages = VkPipelineStageFlags2.VK_PIPELINE_STAGE_2_VERTEX_SHADER_BIT;
                break;
            case ImageLayout.General:
            case ImageLayout.TransferSrc:
            case ImageLayout.TransferDst:
                StartAfterStages = VkPipelineStageFlags2.VK_PIPELINE_STAGE_2_TRANSFER_BIT;
                break;
            case ImageLayout.PresentSrc:
                // Not Sure
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(from), from, null);
        }
    }
}
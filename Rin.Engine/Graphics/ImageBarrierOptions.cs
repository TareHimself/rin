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

    public VkPipelineStageFlags2 WaitForStages = VkPipelineStageFlags2.VK_PIPELINE_STAGE_2_ALL_COMMANDS_BIT;
    public VkPipelineStageFlags2 NextStages = VkPipelineStageFlags2.VK_PIPELINE_STAGE_2_ALL_COMMANDS_BIT;

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
            case ImageFormat.RGB8:
            case ImageFormat.RGB16:
            case ImageFormat.RGB32:
            case ImageFormat.RGBA8:
            case ImageFormat.RGBA16:
            case ImageFormat.RGBA32:
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
                WaitForStages = VkPipelineStageFlags2.VK_PIPELINE_STAGE_2_COLOR_ATTACHMENT_OUTPUT_BIT;
                break;
            case ImageLayout.StencilAttachment:
            case ImageLayout.DepthAttachment:
                WaitForStages = VkPipelineStageFlags2.VK_PIPELINE_STAGE_2_LATE_FRAGMENT_TESTS_BIT;
                break;
            case ImageLayout.ShaderReadOnly:
                WaitForStages = VkPipelineStageFlags2.VK_PIPELINE_STAGE_2_VERTEX_SHADER_BIT;
                break;
            case ImageLayout.General:
            case ImageLayout.TransferSrc:
            case ImageLayout.TransferDst:
                WaitForStages = VkPipelineStageFlags2.VK_PIPELINE_STAGE_2_TRANSFER_BIT;
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
                NextStages = VkPipelineStageFlags2.VK_PIPELINE_STAGE_2_COLOR_ATTACHMENT_OUTPUT_BIT;
                break;
            case ImageLayout.StencilAttachment:
            case ImageLayout.DepthAttachment:
                NextStages = VkPipelineStageFlags2.VK_PIPELINE_STAGE_2_EARLY_FRAGMENT_TESTS_BIT;
                break;
            case ImageLayout.ShaderReadOnly:
                NextStages = VkPipelineStageFlags2.VK_PIPELINE_STAGE_2_VERTEX_SHADER_BIT;
                break;
            case ImageLayout.General:
            case ImageLayout.TransferSrc:
            case ImageLayout.TransferDst:
                NextStages = VkPipelineStageFlags2.VK_PIPELINE_STAGE_2_TRANSFER_BIT;
                break;
            case ImageLayout.PresentSrc:
                // Not Sure
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(from), from, null);
        }
    }
}
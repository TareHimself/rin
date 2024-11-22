using TerraFX.Interop.Vulkan;

namespace rin.Graphics;

/// <summary>
///     Options for image barriers
/// </summary>
public struct ImageBarrierOptions()
{
    public VkAccessFlags2 DstAccessFlags =
        VkAccessFlags2.VK_ACCESS_2_MEMORY_WRITE_BIT | VkAccessFlags2.VK_ACCESS_2_MEMORY_READ_BIT;

    public VkPipelineStageFlags2 NextStages = VkPipelineStageFlags2.VK_PIPELINE_STAGE_2_ALL_COMMANDS_BIT;
    public VkAccessFlags2 SrcAccessFlags = VkAccessFlags2.VK_ACCESS_2_MEMORY_WRITE_BIT;
    public VkPipelineStageFlags2 WaitForStages = VkPipelineStageFlags2.VK_PIPELINE_STAGE_2_ALL_COMMANDS_BIT;

    public VkImageSubresourceRange SubresourceRange =
        SGraphicsModule.MakeImageSubresourceRange(VkImageAspectFlags.VK_IMAGE_ASPECT_COLOR_BIT);
    
}
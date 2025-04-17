using TerraFX.Interop.Vulkan;

namespace Rin.Engine.Graphics;

/// <summary>
///     Options for image barriers
/// </summary>
public struct MemoryBarrierOptions
{
    public VkAccessFlags2 SrcAccessFlags = VkAccessFlags2.VK_ACCESS_2_MEMORY_WRITE_BIT;

    public VkAccessFlags2 DstAccessFlags =
        VkAccessFlags2.VK_ACCESS_2_MEMORY_WRITE_BIT | VkAccessFlags2.VK_ACCESS_2_MEMORY_READ_BIT;

    public VkPipelineStageFlags2 WaitForStages = VkPipelineStageFlags2.VK_PIPELINE_STAGE_2_ALL_COMMANDS_BIT;
    public VkPipelineStageFlags2 NextStages = VkPipelineStageFlags2.VK_PIPELINE_STAGE_2_ALL_COMMANDS_BIT;

    public MemoryBarrierOptions()
    {
    }
    
    public static MemoryBarrierOptions ComputeToTransfer() => new MemoryBarrierOptions
    {
        WaitForStages = VkPipelineStageFlags2.VK_PIPELINE_STAGE_2_COMPUTE_SHADER_BIT,
        NextStages = VkPipelineStageFlags2.VK_PIPELINE_STAGE_2_ALL_TRANSFER_BIT
    };
    
    public static MemoryBarrierOptions TransferToCompute() => new MemoryBarrierOptions
    {
        WaitForStages = VkPipelineStageFlags2.VK_PIPELINE_STAGE_2_ALL_TRANSFER_BIT,
        NextStages = VkPipelineStageFlags2.VK_PIPELINE_STAGE_2_COMPUTE_SHADER_BIT
    };

    public static MemoryBarrierOptions ComputeToGraphics() => new MemoryBarrierOptions
    {
        WaitForStages = VkPipelineStageFlags2.VK_PIPELINE_STAGE_2_COMPUTE_SHADER_BIT,
        NextStages = VkPipelineStageFlags2.VK_PIPELINE_STAGE_2_ALL_GRAPHICS_BIT
    };
    
    public static MemoryBarrierOptions GraphicsToCompute() => new MemoryBarrierOptions
    {
        WaitForStages = VkPipelineStageFlags2.VK_PIPELINE_STAGE_2_ALL_GRAPHICS_BIT,
        NextStages = VkPipelineStageFlags2.VK_PIPELINE_STAGE_2_COMPUTE_SHADER_BIT
    };
    
    public static MemoryBarrierOptions ComputeToAll() => new MemoryBarrierOptions
    {
        WaitForStages = VkPipelineStageFlags2.VK_PIPELINE_STAGE_2_COMPUTE_SHADER_BIT,
        NextStages = VkPipelineStageFlags2.VK_PIPELINE_STAGE_2_ALL_COMMANDS_BIT
    };
}
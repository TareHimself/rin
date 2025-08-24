using TerraFX.Interop.Vulkan;

namespace Rin.Framework.Graphics;

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

    public MemoryBarrierOptions(BufferUsage fromUsage, BufferUsage toUsage, ResourceOperation fromOperation,
        ResourceOperation toOperation)
    {
        WaitForStages = fromUsage switch
        {
            BufferUsage.Undefined => VkPipelineStageFlags2.VK_PIPELINE_STAGE_2_NONE,
            BufferUsage.Host => VkPipelineStageFlags2.VK_PIPELINE_STAGE_2_HOST_BIT,
            BufferUsage.Transfer => VkPipelineStageFlags2.VK_PIPELINE_STAGE_2_ALL_TRANSFER_BIT,
            BufferUsage.Graphics => VkPipelineStageFlags2.VK_PIPELINE_STAGE_2_ALL_GRAPHICS_BIT,
            BufferUsage.Compute => VkPipelineStageFlags2.VK_PIPELINE_STAGE_2_COMPUTE_SHADER_BIT,
            BufferUsage.Indirect => VkPipelineStageFlags2
                .VK_PIPELINE_STAGE_2_ALL_GRAPHICS_BIT, // Graphics pipelines execute after a drawIndirect call
            _ => throw new ArgumentOutOfRangeException(nameof(fromUsage), fromUsage, null)
        };
        NextStages = toUsage switch
        {
            BufferUsage.Undefined => throw new Exception("Buffer cannot transition to undefined stage"),
            BufferUsage.Host => VkPipelineStageFlags2.VK_PIPELINE_STAGE_2_HOST_BIT,
            BufferUsage.Transfer => VkPipelineStageFlags2.VK_PIPELINE_STAGE_2_ALL_TRANSFER_BIT,
            BufferUsage.Graphics => VkPipelineStageFlags2.VK_PIPELINE_STAGE_2_ALL_GRAPHICS_BIT,
            BufferUsage.Compute => VkPipelineStageFlags2.VK_PIPELINE_STAGE_2_COMPUTE_SHADER_BIT,
            BufferUsage.Indirect => VkPipelineStageFlags2.VK_PIPELINE_STAGE_2_DRAW_INDIRECT_BIT,
            _ => throw new ArgumentOutOfRangeException(nameof(fromUsage), fromUsage, null)
        };

        SrcAccessFlags = fromOperation switch
        {
            ResourceOperation.Read => fromUsage switch
            {
                BufferUsage.Undefined => VkAccessFlags2.VK_ACCESS_2_NONE,
                BufferUsage.Host => VkAccessFlags2.VK_ACCESS_2_HOST_READ_BIT,
                BufferUsage.Transfer => VkAccessFlags2.VK_ACCESS_2_TRANSFER_READ_BIT,
                BufferUsage.Graphics or BufferUsage.Compute => VkAccessFlags2.VK_ACCESS_2_SHADER_READ_BIT,
                BufferUsage.Indirect => VkAccessFlags2.VK_ACCESS_2_INDIRECT_COMMAND_READ_BIT,
                _ => throw new ArgumentOutOfRangeException(nameof(fromUsage), fromUsage, null)
            },
            ResourceOperation.Write => fromUsage switch
            {
                BufferUsage.Undefined => VkAccessFlags2.VK_ACCESS_2_NONE,
                BufferUsage.Host => VkAccessFlags2.VK_ACCESS_2_HOST_WRITE_BIT,
                BufferUsage.Transfer => VkAccessFlags2.VK_ACCESS_2_TRANSFER_WRITE_BIT,
                BufferUsage.Graphics or BufferUsage.Compute => VkAccessFlags2.VK_ACCESS_2_SHADER_WRITE_BIT,
                BufferUsage.Indirect => VkAccessFlags2.VK_ACCESS_2_SHADER_WRITE_BIT,
                _ => throw new ArgumentOutOfRangeException(nameof(fromUsage), fromUsage, null)
            },
            _ => throw new ArgumentOutOfRangeException(nameof(fromOperation), fromOperation, null)
        };

        DstAccessFlags = toOperation switch
        {
            ResourceOperation.Read => toUsage switch
            {
                BufferUsage.Host => VkAccessFlags2.VK_ACCESS_2_HOST_READ_BIT,
                BufferUsage.Transfer => VkAccessFlags2.VK_ACCESS_2_TRANSFER_READ_BIT,
                BufferUsage.Graphics or BufferUsage.Compute => VkAccessFlags2.VK_ACCESS_2_SHADER_READ_BIT,
                BufferUsage.Indirect => VkAccessFlags2.VK_ACCESS_2_INDIRECT_COMMAND_READ_BIT,
                _ => throw new ArgumentOutOfRangeException(nameof(toUsage), toUsage, null)
            },
            ResourceOperation.Write => toUsage switch
            {
                BufferUsage.Host => VkAccessFlags2.VK_ACCESS_2_HOST_WRITE_BIT,
                BufferUsage.Transfer => VkAccessFlags2.VK_ACCESS_2_TRANSFER_WRITE_BIT,
                BufferUsage.Graphics or BufferUsage.Compute => VkAccessFlags2.VK_ACCESS_2_SHADER_WRITE_BIT,
                BufferUsage.Indirect => VkAccessFlags2.VK_ACCESS_2_SHADER_WRITE_BIT,
                _ => throw new ArgumentOutOfRangeException(nameof(toUsage), toUsage, null)
            },
            _ => throw new ArgumentOutOfRangeException(nameof(toOperation), toOperation, null)
        };
    }
}
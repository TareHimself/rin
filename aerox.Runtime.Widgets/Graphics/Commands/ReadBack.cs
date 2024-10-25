using aerox.Runtime.Graphics;
using TerraFX.Interop.Vulkan;

namespace aerox.Runtime.Widgets.Graphics.Commands;

/// <summary>
/// Copies the draw image into the copy image to allow for effects like blur
/// </summary>
public class ReadBack : UtilityCommand
{
    // public override void Run(WidgetFrame frame)
    // {
    //     if(frame.IsMainPassActive) frame.Surface.EndActivePass(frame);
    //     var drawImage = frame.Surface.GetDrawImage();
    //     var copyImage = frame.Surface.GetCopyImage();
    //     
    //     var cmd = frame.Raw.GetCommandBuffer();
    //     
    //     drawImage.Barrier(cmd, VkImageLayout.VK_IMAGE_LAYOUT_COLOR_ATTACHMENT_OPTIMAL,
    //         VkImageLayout.VK_IMAGE_LAYOUT_TRANSFER_SRC_OPTIMAL, new ImageBarrierOptions()
    //         {
    //             WaitForStages = VkPipelineStageFlags2.VK_PIPELINE_STAGE_2_COLOR_ATTACHMENT_OUTPUT_BIT,
    //             NextStages = VkPipelineStageFlags2.VK_PIPELINE_STAGE_2_TRANSFER_BIT
    //         });
    //     
    //     copyImage.Barrier(cmd, VkImageLayout.VK_IMAGE_LAYOUT_SHADER_READ_ONLY_OPTIMAL,
    //         VkImageLayout.VK_IMAGE_LAYOUT_TRANSFER_DST_OPTIMAL, new ImageBarrierOptions()
    //         {
    //             WaitForStages = VkPipelineStageFlags2.VK_PIPELINE_STAGE_2_LATE_FRAGMENT_TESTS_BIT,
    //             NextStages = VkPipelineStageFlags2.VK_PIPELINE_STAGE_2_TRANSFER_BIT
    //         });
    //     
    //     drawImage.CopyTo(cmd,copyImage);
    //
    //     drawImage.Barrier(cmd, VkImageLayout.VK_IMAGE_LAYOUT_TRANSFER_SRC_OPTIMAL,
    //         VkImageLayout.VK_IMAGE_LAYOUT_COLOR_ATTACHMENT_OPTIMAL, new ImageBarrierOptions()
    //         {
    //             WaitForStages = VkPipelineStageFlags2.VK_PIPELINE_STAGE_2_TRANSFER_BIT,
    //             NextStages = VkPipelineStageFlags2.VK_PIPELINE_STAGE_2_COLOR_ATTACHMENT_OUTPUT_BIT
    //         });
    //     copyImage.Barrier(cmd, VkImageLayout.VK_IMAGE_LAYOUT_TRANSFER_DST_OPTIMAL,
    //         VkImageLayout.VK_IMAGE_LAYOUT_SHADER_READ_ONLY_OPTIMAL,
    //         new ImageBarrierOptions()
    //         {
    //             WaitForStages = VkPipelineStageFlags2.VK_PIPELINE_STAGE_2_TRANSFER_BIT,
    //             NextStages = VkPipelineStageFlags2.VK_PIPELINE_STAGE_2_VERTEX_INPUT_BIT
    //         });
    // }
}
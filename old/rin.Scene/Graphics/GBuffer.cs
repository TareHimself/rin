using rin.Runtime.Graphics;
using rin.Runtime.Core.Math;
using TerraFX.Interop.Vulkan;

namespace rin.Scene.Graphics;

public class GBuffer
{
    public DeviceImage Color;
    public DeviceImage Location;
    public DeviceImage Normal;
    public DeviceImage RoughnessMetallicSpecular;
    public DeviceImage Emissive;


    public GBuffer(DeviceImage color, DeviceImage location, DeviceImage normal, DeviceImage roughnessMetallicSpecular, DeviceImage emissive)
    {
        Color = color;
        Location = location;
        Normal = normal;
        RoughnessMetallicSpecular = roughnessMetallicSpecular;
        Emissive = emissive;
    }

    public void Dispose()
    {
        Color.Dispose();
        Location.Dispose();
        Normal.Dispose();
        RoughnessMetallicSpecular.Dispose();
        Emissive.Dispose();
    }

    public void Barrier(VkCommandBuffer cmd, VkImageLayout from, VkImageLayout to,
        ImageBarrierOptions? options = null)
    {
        Color.Barrier(cmd, from, to, options);
        Location.Barrier(cmd, from, to, options);
        Normal.Barrier(cmd, from, to, options);
        RoughnessMetallicSpecular.Barrier(cmd, from, to, options);
        Emissive.Barrier(cmd, from, to, options);
    }
    
    
    public VkRenderingAttachmentInfo[] MakeAttachments() {
        var clearValue = new VkClearValue()
        {
            color = SGraphicsModule.MakeClearColorValue(0.0f)
        };

        return
        [
            SGraphicsModule.MakeRenderingAttachment(Color.View, VkImageLayout.VK_IMAGE_LAYOUT_COLOR_ATTACHMENT_OPTIMAL,
                clearValue),
            SGraphicsModule.MakeRenderingAttachment(Location.View, VkImageLayout.VK_IMAGE_LAYOUT_COLOR_ATTACHMENT_OPTIMAL,
                clearValue),
            SGraphicsModule.MakeRenderingAttachment(Normal.View, VkImageLayout.VK_IMAGE_LAYOUT_COLOR_ATTACHMENT_OPTIMAL,
                clearValue),
            SGraphicsModule.MakeRenderingAttachment(RoughnessMetallicSpecular.View, VkImageLayout.VK_IMAGE_LAYOUT_COLOR_ATTACHMENT_OPTIMAL,
                clearValue),
            SGraphicsModule.MakeRenderingAttachment(Emissive.View, VkImageLayout.VK_IMAGE_LAYOUT_COLOR_ATTACHMENT_OPTIMAL,
                clearValue)
        ];
    }
}
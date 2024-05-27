using aerox.Runtime.Graphics;
using TerraFX.Interop.Vulkan;

namespace aerox.Runtime.Scene.Graphics.Drawers.Deferred;

public struct GBuffer
{
    public DeviceImage Color;
    public DeviceImage Location;
    public DeviceImage Normal;
    public DeviceImage RoughnessMetallicSpecular;
    public DeviceImage Emissive;

    public void Dispose()
    {
        Color.Dispose();
        Location.Dispose();
        Normal.Dispose();
        RoughnessMetallicSpecular.Dispose();
        Emissive.Dispose();
    }

    public void ImageBarrier(VkCommandBuffer cmd, VkImageLayout from, VkImageLayout to,
        ImageBarrierOptions? options = null)
    {
        SGraphicsModule.ImageBarrier(cmd, Color, from, to, options);
        SGraphicsModule.ImageBarrier(cmd, Location, from, to, options);
        SGraphicsModule.ImageBarrier(cmd, Normal, from, to, options);
        SGraphicsModule.ImageBarrier(cmd, RoughnessMetallicSpecular, from, to, options);
        SGraphicsModule.ImageBarrier(cmd, Emissive, from, to, options);
    }
}
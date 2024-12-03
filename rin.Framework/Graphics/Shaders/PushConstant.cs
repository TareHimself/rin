using TerraFX.Interop.Vulkan;

namespace rin.Framework.Graphics.Shaders;

public class PushConstant
{
    public string Name = "";
    public uint Size;
    public VkShaderStageFlags Stages;
}
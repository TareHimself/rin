using TerraFX.Interop.Vulkan;

namespace Rin.Engine.Graphics.Shaders;

public class PushConstant
{
    public string Name = "";
    public uint Size;
    public ShaderStage Stages;
}
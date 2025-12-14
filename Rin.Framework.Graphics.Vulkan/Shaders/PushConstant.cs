using Rin.Framework.Graphics.Shaders;

namespace Rin.Framework.Graphics.Vulkan.Shaders;

public class PushConstant
{
    public string Name = "";
    public uint Size;
    public ShaderStage Stages;
}
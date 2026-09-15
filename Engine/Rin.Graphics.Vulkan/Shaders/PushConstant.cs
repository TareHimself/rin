using Rin.Core.Graphics.Shaders;

namespace Rin.Graphics.Vulkan.Shaders;

public class PushConstant
{
    public string Name = "";
    public uint Size;
    public ShaderStage Stages;
}
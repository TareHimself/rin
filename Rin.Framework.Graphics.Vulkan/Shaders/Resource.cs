using Rin.Framework.Graphics.Shaders;

namespace Rin.Framework.Graphics.Vulkan.Shaders;

public class Resource
{
    public uint Binding;
    public DescriptorBindingFlags BindingFlags;
    public uint Count;
    public string Name = "";
    public uint Set;
    public uint Size = 0;
    public ShaderStage Stages;
    public DescriptorType Type;
}
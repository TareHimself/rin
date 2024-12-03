using TerraFX.Interop.Vulkan;

namespace rin.Framework.Graphics.Shaders;

public class Resource
{
    public string Name = "";
    public uint Set;
    public uint Binding;
    public uint Count;
    public VkDescriptorType Type;
    public VkShaderStageFlags Stages;
    public VkDescriptorBindingFlags BindingFlags;
    public uint Size = 0;
}
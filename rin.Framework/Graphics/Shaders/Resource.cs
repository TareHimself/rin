using TerraFX.Interop.Vulkan;

namespace rin.Framework.Graphics.Shaders;

public class Resource
{
    public uint Binding;
    public VkDescriptorBindingFlags BindingFlags;
    public uint Count;
    public string Name = "";
    public uint Set;
    public uint Size = 0;
    public VkShaderStageFlags Stages;
    public VkDescriptorType Type;
}
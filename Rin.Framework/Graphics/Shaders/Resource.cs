namespace Rin.Framework.Graphics.Shaders;

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
using aerox.Runtime.Graphics.Shaders;

namespace aerox.Runtime.Graphics.Material;

public class MaterialResources
{
    public Dictionary<string, ShaderModule.Texture> Textures = [];
    public Dictionary<string,ShaderModule.Buffer> Buffers = [];
    public Dictionary<string,ShaderModule.PushConstant> PushConstants = [];
}
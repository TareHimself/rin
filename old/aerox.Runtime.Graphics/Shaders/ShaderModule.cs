using System.Collections;
using aerox.Runtime.Graphics.Descriptors;
using aerox.Runtime.Graphics.Material;
using ashl.Parser;
using TerraFX.Interop.Vulkan;
using static TerraFX.Interop.Vulkan.Vulkan;

namespace aerox.Runtime.Graphics.Shaders;

public class ShaderModule : Disposable
{
    
    public struct Texture
    {
        public string Name;
        public uint Set;
        public uint Binding;
        public uint Count;
    }
        
    public struct Buffer
    {
        public string Name;
        public uint Set;
        public uint Binding;
        public int Size;
        public uint Count;
    }
        
    public struct PushConstant
    {
        public string Name;
        public int Size;
        public VkShaderStageFlags Stages;
    }
    
    public readonly List<Node> Source;
    public readonly EScopeType ScopeType;

    // public VkShaderModule Module
    // {
    //     get;
    //     private set;
    // }

    public VkShaderStageFlags StageFlags => ScopeType switch
    {
        EScopeType.Vertex => VkShaderStageFlags.VK_SHADER_STAGE_VERTEX_BIT,
        EScopeType.Fragment => VkShaderStageFlags.VK_SHADER_STAGE_FRAGMENT_BIT,
        _ => throw new ArgumentOutOfRangeException(nameof(ScopeType), ScopeType, null)
    };


    public readonly Dictionary<string, Texture> Textures = [];
    
    public readonly Dictionary<string, Buffer> Buffers = [];
    
    public readonly Dictionary<string, PushConstant> PushConstants = [];
    
    public string Id { private set; get; }
    
    public ShaderModule(List<Node> nodes,EScopeType scopeType)
    {
        Source = nodes;
        Id = Source.GetHashCode().ToString();
        ScopeType = scopeType;
        
        Dictionary<MaterialInstance.SetType, DescriptorLayoutBuilder> layoutBuilders = new();
        
        foreach (var node in nodes)
        {
            if (node is LayoutNode asLayout)
            {
                if (asLayout.Declaration.DeclarationType == EDeclarationType.Block)
                {
                    var asBlock = (BlockDeclarationNode)asLayout.Declaration;
                    
                    var item = new Buffer()
                    {
                        Name = asBlock.Name,
                        Set = uint.Parse(asLayout.Tags["set"]),
                        Binding = uint.Parse(asLayout.Tags["binding"]),
                        Size = asBlock.SizeOf(),
                        Count = (uint)asLayout.Declaration.Count
                    };

                    Buffers.Add(asBlock.Name,item);
                }
                else if(asLayout.Declaration.DeclarationType == EDeclarationType.Sampler2D)
                {
                    var item = new Texture()
                    {
                        Name = asLayout.Declaration.Name,
                        Set = uint.Parse(asLayout.Tags["set"]),
                        Binding = uint.Parse(asLayout.Tags["binding"]),
                        Count = (uint)asLayout.Declaration.Count
                    };

                    Textures.Add(asLayout.Declaration.Name,item);
                }
            }
            else if(node is PushConstantNode asPushConstant)
            {
                PushConstants.Add(asPushConstant.Name,new PushConstant()
                {
                    Name = asPushConstant.Name,
                    Size = asPushConstant.Data.Declarations.Aggregate(0, (total, decl) => decl.SizeOf() + total),
                    Stages = StageFlags
                });
            }
        }
    }

    public IEnumerable<VkPushConstantRange> ComputePushConstantRanges() => PushConstants.Select(c => new VkPushConstantRange()
    {
        stageFlags = c.Value.Stages,
        offset = 0,
        size = (uint)c.Value.Size
    });
    
    
    protected override void OnDispose(bool isManual)
    {

    }
    
    
}
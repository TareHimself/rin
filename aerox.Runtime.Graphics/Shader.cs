using System.Text.Json.Nodes;
using ashl.Parser;
using shaderc;
using TerraFX.Interop.Vulkan;
using static TerraFX.Interop.Vulkan.Vulkan;

namespace aerox.Runtime.Graphics;

/// <summary>
///     A shader
/// </summary>
public class Shader : MultiDisposable
{
    
    public struct Stage : IDisposable
    {
        public class Texture
        {
            public string Name;
            public uint Set;
            public uint Binding;
            public uint Count;
        }
        
        public class Buffer
        {
            public string Name;
            public uint Set;
            public uint Binding;
            public int Size;
            public uint Count;
        }
        
        public class PushConstant
        {
            public string Name;
            public int Size;
            public VkShaderStageFlags Stages;
        }

        public class Resources
        {
            public Dictionary<string, Texture> Textures = [];
            public Dictionary<string, Buffer> Buffers = [];
            public PushConstant? Push;
        }


        private readonly VkShaderModule _module;
        public readonly VkShaderStageFlags Flags;
        private readonly Resources _resources;

        public Stage(VkShaderModule shaderModule, VkShaderStageFlags stageFlags, List<Node> nodes)
        {
            _module = shaderModule;
            Flags = stageFlags;
            var resources = new Resources();
            foreach (var node in nodes)
            {
                if (node is LayoutNode asLayout)
                {
                    if (asLayout.Declaration.DeclarationType == EDeclarationType.Block)
                    {
                        var asBlock = (BlockDeclarationNode)asLayout.Declaration;
                        resources.Buffers.Add(asBlock.Name,new Buffer()
                        {
                            Name = asBlock.Name,
                            Set = uint.Parse(asLayout.Tags["set"]),
                            Binding = uint.Parse(asLayout.Tags["binding"]),
                            Size = asBlock.SizeOf(),
                            Count = (uint)asLayout.Declaration.Count
                        });
                    }
                    else if(asLayout.Declaration.DeclarationType == EDeclarationType.Sampler2D)
                    {
                        resources.Textures.Add(asLayout.Declaration.Name,new Texture()
                        {
                            Name = asLayout.Declaration.Name,
                            Set = uint.Parse(asLayout.Tags["set"]),
                            Binding = uint.Parse(asLayout.Tags["binding"]),
                            Count = (uint)asLayout.Declaration.Count
                        });
                    }
                }
                else if(node is PushConstantNode asPushConstant)
                {
                    resources.Push = new PushConstant()
                    {
                        Name = asPushConstant.Name,
                        Size = asPushConstant.Data.Declarations.Aggregate(0, (total, decl) => decl.SizeOf() + total),
                        Stages = stageFlags
                    };
                }
            }

            _resources = resources;
        }
        
        public static implicit operator VkShaderModule(Stage s)
        {
            return s._module;
        }

        public void Dispose()
        {
            unsafe
            {
                vkDestroyShaderModule(SGraphicsModule.Get().GetDevice(), _module, null);
            }
        }

        public Resources GetResources() => _resources;
    }

    private readonly Stage[] _stages;
    
    public Shader(string sourceFile, Stage[] stages)
    {
        _stages = stages;
    }

    public Stage[] GetStages()
    {
        return _stages;
    }

    protected override void OnDispose(bool isManual)
    {
        foreach (var stage in _stages)
        {
            stage.Dispose();
        }
    }
}
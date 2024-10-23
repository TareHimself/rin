using aerox.Runtime.Graphics.Descriptors;
using rsl.Nodes;
using TerraFX.Interop.Vulkan;

namespace aerox.Runtime.Graphics.Shaders;

public abstract class Shader : Disposable
{
    
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
    
    public class PushConstant
    {
        public string Name = "";
        public ulong Size;
        public VkShaderStageFlags Stages;
    }

    public Dictionary<string, Resource> Resources = [];
    public Dictionary<string, PushConstant> PushConstants = [];
    
    public abstract bool Bind(VkCommandBuffer cmd,bool wait = false);

    public abstract CompiledShader Compile(ShaderManager manager);

    protected void ComputeResources(Pair<VkShaderStageFlags, ModuleNode>[] shaders)
    {
        foreach (var (shaderStages,shader) in shaders)
        {
            foreach (var node in shader.Statements)
            {
                switch (node.NodeType)
                {
                    case NodeType.Layout:
                    {
                        if (node is LayoutNode asLayout && asLayout.LayoutType == LayoutType.Uniform)
                        {
                            VkDescriptorType type = asLayout.Declaration.DeclarationType switch
                            {
                                DeclarationType.Block => asLayout.Tags.ContainsKey("$storage") ? VkDescriptorType.VK_DESCRIPTOR_TYPE_STORAGE_BUFFER : VkDescriptorType.VK_DESCRIPTOR_TYPE_UNIFORM_BUFFER,
                                DeclarationType.Sampler2D => VkDescriptorType.VK_DESCRIPTOR_TYPE_COMBINED_IMAGE_SAMPLER,
                                _ => throw new Exception("Unexpected declaration type")
                            };

                            var set = uint.Parse(asLayout.Tags["set"]);
                            var binding = uint.Parse(asLayout.Tags["binding"]);

                            VkDescriptorBindingFlags bindingFlags = 0;
                            VkShaderStageFlags stages = 0;

                            if (asLayout.Tags.ContainsKey("$update"))
                            {
                                bindingFlags |= VkDescriptorBindingFlags.VK_DESCRIPTOR_BINDING_UPDATE_AFTER_BIND_BIT;
                            }
                            
                            if (asLayout.Tags.ContainsKey("$partial"))
                            {
                                bindingFlags |= VkDescriptorBindingFlags.VK_DESCRIPTOR_BINDING_PARTIALLY_BOUND_BIT;
                            }
                            
                            if (asLayout.Tags.ContainsKey("$variable"))
                            {
                                bindingFlags |= VkDescriptorBindingFlags.VK_DESCRIPTOR_BINDING_VARIABLE_DESCRIPTOR_COUNT_BIT;
                            }
                            
                            if (asLayout.Tags.ContainsKey("$stage") && asLayout.Tags["$stage"] == "all")
                            {
                                stages |= VkShaderStageFlags.VK_SHADER_STAGE_ALL;
                            }

                            var name = asLayout.Declaration.DeclarationName;

                            if (Resources.ContainsKey(name))
                            {
                                Resources[name].Stages |= stages;
                                Resources[name].BindingFlags |= bindingFlags;
                            }
                            else
                            {
                                var count = (uint)asLayout.Declaration.Count;

                                if (asLayout.Tags.ContainsKey("$variable"))
                                {
                                    count = uint.Parse(asLayout.Tags["$variable"]);
                                }

                                if (asLayout.Declaration.DeclarationType == DeclarationType.Block)
                                {
                                    Resources.Add(name,new Resource()
                                    {
                                        Name = name,
                                        Set = set,
                                        Binding = binding,
                                        Count = count,
                                        Type = type,
                                        Stages = stages,
                                        BindingFlags = bindingFlags,
                                        Size = (uint)asLayout.Declaration.SizeOf()
                                    });
                                }
                                else
                                {
                                    Resources.Add(name,new Resource()
                                    {
                                        Name = name,
                                        Set = set,
                                        Binding = binding,
                                        Count = count,
                                        Type = type,
                                        Stages = stages,
                                        BindingFlags = bindingFlags
                                    });
                                }
                            }
                        }
                    }
                        break;
                    case NodeType.PushConstant:
                    {
                        if (node is PushConstantNode asPushConstant)
                        {
                            var name = "push";
                            if (PushConstants.TryGetValue(name, out var constant))
                            {
                                constant.Stages |= shaderStages;
                            }
                            else
                            {
                                ulong pushSize = 0;
                                foreach (var declarationNode in asPushConstant.Declarations)
                                {
                                    pushSize += (ulong)declarationNode.SizeOf();
                                }
                                
                                PushConstants.Add(name,new PushConstant()
                                {
                                    Name = name,
                                    Size = pushSize,
                                    Stages = shaderStages
                                });
                            }
                        }
                    }
                        break;
                    default:
                        break;
                }
            }
        }
    }

    public abstract void Init();
    public abstract Dictionary<uint, VkDescriptorSetLayout> GetDescriptorSetLayouts();
    public abstract VkPipelineLayout GetPipelineLayouts();
    protected override void OnDispose(bool isManual)
    {
        throw new NotImplementedException();
    }
}
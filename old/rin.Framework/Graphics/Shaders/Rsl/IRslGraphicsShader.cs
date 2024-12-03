using rin.Framework.Core;
using rsl.Nodes;
using TerraFX.Interop.Vulkan;

namespace rin.Framework.Graphics.Shaders.Rsl;

public interface IRslGraphicsShader : IGraphicsShader
{
    public void ComputeResources(Pair<VkShaderStageFlags, ModuleNode>[] shaders)
    {
        foreach (var (shaderStages, shader) in shaders)
        {
            foreach (var node in shader.Statements)
            {
                switch (node.NodeType)
                {
                    case NodeType.Layout:
                    case NodeType.SSBO:
                    {
                        uint set = 0;
                        uint binding = 0;
                        VkDescriptorBindingFlags bindingFlags = 0;
                        VkShaderStageFlags stages = 0;
                        Dictionary<string, string> tags = [];
                        VkDescriptorType type = VkDescriptorType.VK_DESCRIPTOR_TYPE_MAX_ENUM;
                        var name = "";
                        uint count = 0;
                        uint size = 0;
                        {
                            if (node is LayoutNode resource)
                            {
                                if (!resource.Tags.TryGetValue("set", out var layoutSet) ||
                                    !resource.Tags.TryGetValue("binding", out var layoutBinding))
                                {
                                    continue;
                                }

                                switch (resource.Declaration.DeclarationType)
                                {
                                    case DeclarationType.Block:
                                    {
                                        type = VkDescriptorType.VK_DESCRIPTOR_TYPE_UNIFORM_BUFFER;
                                        size = (uint)resource.Declaration.SizeOf();
                                    }
                                        break;
                                    case DeclarationType.Sampler2D:
                                    {
                                        type = VkDescriptorType.VK_DESCRIPTOR_TYPE_COMBINED_IMAGE_SAMPLER;
                                        size = 0;
                                    }
                                        break;
                                    default:
                                        throw new Exception("Unexpected declaration type");
                                }
                                
                                set = uint.Parse(layoutSet);
                                binding = uint.Parse(layoutBinding);
                                tags = resource.Tags;
                                name = resource.Declaration.DeclarationName;
                                count = (uint)resource.Declaration.Count;
                            }
                        }

                        {
                            if (node is SSBONode resource)
                            {
                                if (!resource.Tags.TryGetValue("set", out var layoutSet) ||
                                    !resource.Tags.TryGetValue("binding", out var layoutBinding))
                                {
                                    continue;
                                }
                                
                                type = VkDescriptorType.VK_DESCRIPTOR_TYPE_STORAGE_BUFFER;

                                set = uint.Parse(layoutSet);
                                binding = uint.Parse(layoutBinding);
                                tags = resource.Tags;
                                name = resource.Name;
                                count = 1;
                                size = (uint)resource.SizeOf();
                            }
                        }

                        if (type == VkDescriptorType.VK_DESCRIPTOR_TYPE_MAX_ENUM)
                        {
                            throw new Exception("Unknown descriptor type");
                        }


                        if (tags.ContainsKey("$update"))
                        {
                            bindingFlags |= VkDescriptorBindingFlags.VK_DESCRIPTOR_BINDING_UPDATE_AFTER_BIND_BIT;
                        }

                        if (tags.ContainsKey("$partial"))
                        {
                            bindingFlags |= VkDescriptorBindingFlags.VK_DESCRIPTOR_BINDING_PARTIALLY_BOUND_BIT;
                        }

                        if (tags.ContainsKey("$variable"))
                        {
                            bindingFlags |= VkDescriptorBindingFlags
                                .VK_DESCRIPTOR_BINDING_VARIABLE_DESCRIPTOR_COUNT_BIT;
                        }

                        if (tags.ContainsKey("$stage") && tags["$stage"] == "all")
                        {
                            stages |= VkShaderStageFlags.VK_SHADER_STAGE_ALL;
                        }

                        if (Resources.ContainsKey(name))
                        {
                            Resources[name].Stages |= stages;
                            Resources[name].BindingFlags |= bindingFlags;
                        }
                        else
                        {
                            if (tags.TryGetValue("$variable", out var tag))
                            {
                                tag = tag.Trim();
                                count = uint.Parse(tag.Length == 0 ? "0" : tag);
                            }

                            Resources.Add(name, new Resource()
                            {
                                Name = name,
                                Set = set,
                                Binding = binding,
                                Count = count,
                                Type = type,
                                Stages = stages,
                                BindingFlags = bindingFlags,
                                Size = size
                            });
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
                                uint pushSize = 0;
                                foreach (var declarationNode in asPushConstant.Declarations)
                                {
                                    pushSize += (uint)declarationNode.SizeOf();
                                }

                                PushConstants.Add(name, new PushConstant()
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

}
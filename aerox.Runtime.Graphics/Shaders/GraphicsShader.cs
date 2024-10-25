using aerox.Runtime.Graphics.Descriptors;
using rsl;
using rsl.Nodes;
using TerraFX.Interop.Vulkan;
using static TerraFX.Interop.Vulkan.Vulkan;
namespace aerox.Runtime.Graphics.Shaders;

public class GraphicsShader : Shader
{
    private string _vertexShaderId = "";
    private string _fragmentShaderId = "";
    private ModuleNode _vertexShader;
    private ModuleNode _fragmentShader;
    private Task<CompiledShader> _compiledShader;


    public override CompiledShader Compile(ShaderManager manager)
    {
        var vertexSpirv = manager.CompileAstToSpirv(_vertexShaderId, VkShaderStageFlags.VK_SHADER_STAGE_VERTEX_BIT,
            _vertexShader);
        var fragmentSpirv = manager.CompileAstToSpirv(_fragmentShaderId,
            VkShaderStageFlags.VK_SHADER_STAGE_FRAGMENT_BIT, _fragmentShader);

        var layoutBuilder = new DescriptorLayoutBuilder();
        List<VkPushConstantRange> pushConstantRanges = [];
        SortedDictionary<uint, DescriptorLayoutBuilder> builders = [];
        var shader = new CompiledShader();
        
        foreach (var (key, item) in Resources)
        {
            if (!builders.ContainsKey(item.Set))
            {
                builders.Add(item.Set,new DescriptorLayoutBuilder());
            }

            builders[item.Set].AddBinding(item.Binding, item.Type, item.Stages, item.Count, item.BindingFlags);
        }
        
        foreach (var (key, item) in PushConstants)
        {
            pushConstantRanges.Add(new VkPushConstantRange()
            {
                stageFlags = item.Stages,
                offset = 0,
                size = (uint)item.Size
            });
        }

        var max = builders.Count == 0 ? 0 : builders.Keys.Max();
        List<VkDescriptorSetLayout> layouts = [];

        for (uint i = 0; i < max + 1; i++)
        {
            var newLayout = builders.TryGetValue(i, out var value) ? value.Build() : new DescriptorLayoutBuilder().Build();
            shader.DescriptorLayouts.Add(i,newLayout);
            layouts.Add(newLayout);
        }

        var device = SGraphicsModule.Get().GetDevice();
        unsafe {
            fixed (VkDescriptorSetLayout * pSetLayouts = layouts.ToArray())
            {
                var setLayoutCount = (uint)layouts.Count;
                var pushConstantRangeCount = (uint)pushConstantRanges.Count;
                fixed (VkPushConstantRange * pPushConstants = pushConstantRanges.ToArray())
                {
                    {
                        var pipelineLayoutCreateInfo = new VkPipelineLayoutCreateInfo()
                        {
                            sType = VkStructureType.VK_STRUCTURE_TYPE_PIPELINE_LAYOUT_CREATE_INFO,
                            setLayoutCount = setLayoutCount,
                            pSetLayouts = pSetLayouts,
                            pushConstantRangeCount = pushConstantRangeCount,
                            pPushConstantRanges = pPushConstants
                        };

                        var pipelineLayout = new VkPipelineLayout();
                        vkCreatePipelineLayout(device, &pipelineLayoutCreateInfo, null, &pipelineLayout);
                        shader.PipelineLayout = pipelineLayout;
                    }
                    fixed(byte * pName = "main"u8.ToArray())
                    {
                        {
                            var createInfo = new VkShaderCreateInfoEXT()
                            {
                                sType = VkStructureType.VK_STRUCTURE_TYPE_SHADER_CREATE_INFO_EXT,
                                stage = VkShaderStageFlags.VK_SHADER_STAGE_VERTEX_BIT,
                                nextStage = VkShaderStageFlags.VK_SHADER_STAGE_FRAGMENT_BIT,
                                codeType = VkShaderCodeTypeEXT.VK_SHADER_CODE_TYPE_SPIRV_EXT,
                                codeSize = (nuint)vertexSpirv.GetByteSize(),
                                pCode = vertexSpirv.GetData(),
                                pName = (sbyte*)pName,
                                pushConstantRangeCount = pushConstantRangeCount,
                                setLayoutCount = setLayoutCount,
                                pPushConstantRanges = pPushConstants,
                                pSetLayouts = pSetLayouts,
                                pSpecializationInfo = null,
                                pNext = null
                            };
                            var result = new VkShaderEXT();
                            if (VulkanExtensions.vkCreateShadersEXT(device, 1, &createInfo, null, &result) !=
                                VkResult.VK_SUCCESS)
                            {
                                throw new Exception("Failed to compile shader");
                            }
                        
                            shader.Shaders.Add(new Pair<VkShaderEXT,VkShaderStageFlags>(result,VkShaderStageFlags.VK_SHADER_STAGE_VERTEX_BIT));
                        }
                        
                        {
                            var createInfo = new VkShaderCreateInfoEXT()
                            {
                                sType = VkStructureType.VK_STRUCTURE_TYPE_SHADER_CREATE_INFO_EXT,
                                stage = VkShaderStageFlags.VK_SHADER_STAGE_FRAGMENT_BIT,
                                nextStage = 0,
                                codeType = VkShaderCodeTypeEXT.VK_SHADER_CODE_TYPE_SPIRV_EXT,
                                codeSize = (nuint)fragmentSpirv.GetByteSize(),
                                pCode = fragmentSpirv.GetData(),
                                pName = (sbyte*)pName,
                                pushConstantRangeCount = pushConstantRangeCount,
                                setLayoutCount = setLayoutCount,
                                pPushConstantRanges = pPushConstants,
                                pSetLayouts = pSetLayouts
                            };
                            var result = new VkShaderEXT();
                            if (VulkanExtensions.vkCreateShadersEXT(device, 1, &createInfo, null, &result) !=
                                VkResult.VK_SUCCESS)
                            {
                                throw new Exception("Failed to compile shader");
                            }
                        
                            shader.Shaders.Add(new Pair<VkShaderEXT,VkShaderStageFlags>(result,VkShaderStageFlags.VK_SHADER_STAGE_FRAGMENT_BIT));
                        }
                    }
                }
            }

            manager.BeforeDispose += () =>
            {
                vkDestroyPipelineLayout(device, shader.PipelineLayout, null);
                foreach (var (shaderObj,_ ) in shader.Shaders)
                {
                    VulkanExtensions.vkDestroyShaderEXT(device, shaderObj, null);
                }
            };

            return shader;
        }
    }

   


    public GraphicsShader(ModuleNode vertexShader,ModuleNode fragmentShader)
    {
        _vertexShader = vertexShader;
        _fragmentShader = fragmentShader;
        _vertexShaderId = vertexShader.GetHashCode().ToString();
        _fragmentShaderId = fragmentShader.GetHashCode().ToString();
    }

    public override void Init()
    {
        ComputeResources([new Pair<VkShaderStageFlags, ModuleNode>(VkShaderStageFlags.VK_SHADER_STAGE_VERTEX_BIT,_vertexShader),new Pair<VkShaderStageFlags, ModuleNode>(VkShaderStageFlags.VK_SHADER_STAGE_FRAGMENT_BIT,_fragmentShader)]);

        _compiledShader = SGraphicsModule.Get().GetShaderManager().StartShaderCompilation(this);
    }
    
    public override bool Bind(VkCommandBuffer cmd, bool wait = false)
    {
        if (wait && !_compiledShader.IsCompleted)
        {
            _compiledShader.Wait();
        }
        else if(!_compiledShader.IsCompleted)
        {
            return false;
        }

        cmd.UnBindShader(VkShaderStageFlags.VK_SHADER_STAGE_GEOMETRY_BIT);
        cmd.BindShaders(_compiledShader.Result.Shaders);

        return true;
    }

    public override Dictionary<uint, VkDescriptorSetLayout> GetDescriptorSetLayouts()
    {
        return _compiledShader.Result.DescriptorLayouts;
    }

    public override VkPipelineLayout GetPipelineLayout()
    {
        return _compiledShader.Result.PipelineLayout;
    }

    private static readonly Dictionary<string, GraphicsShader> GraphicsShaders = [];
    
    public static GraphicsShader FromFile(string filePath)
    {
        var fullPath = Path.GetFullPath(filePath);

        if (GraphicsShaders.ContainsKey(filePath)) return GraphicsShaders[fullPath];
        
        var tokenList = Tokenizer.Run(fullPath);

        var ast = Parser.Parse(ref tokenList);
        ast = ast.ResolveIncludes((node, moduleNode) =>
        {
            if (Path.IsPathRooted(node.File)) return Path.GetFullPath(node.File);

            return Path.GetFullPath(node.File,
                Directory.GetParent(node.SourceFile)?.FullName ?? Directory.GetCurrentDirectory());
        });
        ast.ResolveStructReferences();
        var vertexShader = ast.ExtractScope(ScopeType.Vertex);
        var fragmentShader = ast.ExtractScope(ScopeType.Fragment);

        var shader = new GraphicsShader(vertexShader, fragmentShader);
            
        GraphicsShaders.Add(fullPath,shader);
            
        shader.Init();
        return shader;
    }
}
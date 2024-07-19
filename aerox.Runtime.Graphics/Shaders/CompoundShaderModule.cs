using aerox.Runtime.Graphics.Descriptors;
using aerox.Runtime.Graphics.Material;
using TerraFX.Interop.Vulkan;
using static TerraFX.Interop.Vulkan.Vulkan;
namespace aerox.Runtime.Graphics.Shaders;

/// <summary>
/// Creates a shader usable my materials, requires <see cref="SGraphicsModule"/> to be loaded and initialized
/// </summary>
public class CompoundShaderModule : Disposable
{
    public enum ParameterType
    {
        Texture,
        Buffer
    }
    public struct ParameterInfo
    {
        public uint Binding;
        public uint Set;
        public uint Count;
        public ParameterType Type;
    }
    
    public string Id { get; private set; }
    public ShaderModule[] Shaders { get; private set; }
    public readonly Dictionary<uint, VkDescriptorSetLayout> Layouts = [];
    
    public readonly Dictionary<string, ShaderModule.PushConstant> PushConstants = [];
    public VkPipelineLayout PipelineLayout { get; private set; }
    public readonly VkPushConstantRange[] PushConstantRanges;
    public readonly Dictionary<string, ParameterInfo> Parameters = [];
    
    public CompoundShaderModule(IEnumerable<ShaderModule> shaders)
    {
        Shaders = shaders.ToArray();
        Id = Shaders.Aggregate("", (t, c) => t + c.Id);
        Dictionary<uint, DescriptorLayoutBuilder> descriptorLayoutBuilders = [];
        foreach (var shaderModule in Shaders)
        {
            foreach (var item in shaderModule.Textures)
            {
                if(!descriptorLayoutBuilders.ContainsKey(item.Value.Set)) descriptorLayoutBuilders.Add(item.Value.Set,new DescriptorLayoutBuilder());
                
                descriptorLayoutBuilders[item.Value.Set].AddBinding(item.Value.Binding,
                    VkDescriptorType.VK_DESCRIPTOR_TYPE_COMBINED_IMAGE_SAMPLER, shaderModule.StageFlags, item.Value.Count);

                if (!Parameters.ContainsKey(item.Key))
                {
                    Parameters.Add(item.Key,new ParameterInfo()
                    {
                        Binding = item.Value.Binding,
                        Set = item.Value.Set,
                        Count = item.Value.Count,
                        Type = ParameterType.Texture
                    });
                }
            }
            
            foreach (var item in shaderModule.Buffers)
            {
                if(!descriptorLayoutBuilders.ContainsKey(item.Value.Set)) descriptorLayoutBuilders.Add(item.Value.Set,new DescriptorLayoutBuilder());

                descriptorLayoutBuilders[item.Value.Set].AddBinding(item.Value.Binding,
                    VkDescriptorType.VK_DESCRIPTOR_TYPE_UNIFORM_BUFFER, shaderModule.StageFlags, item.Value.Count);
                
                if (!Parameters.ContainsKey(item.Key))
                {
                    Parameters.Add(item.Key,new ParameterInfo()
                    {
                        Binding = item.Value.Binding,
                        Set = item.Value.Set,
                        Count = item.Value.Count,
                        Type = ParameterType.Buffer
                    });
                }
            }
        }

        var maxSet = descriptorLayoutBuilders.Select(c => c.Key).Max();

        for (uint i = 0; i < maxSet + 1; i++)
        {
            var layout = descriptorLayoutBuilders.TryGetValue(i, out var builder)
                ? builder.Build()
                : new DescriptorLayoutBuilder().Build();

            Layouts.Add(i, layout);
        }

        foreach (var pushConstant in Shaders.SelectMany(c => c.PushConstants).GroupBy(c => c.Value.Size).Select(c =>
                     c.Aggregate(c.First().Value, (total, current) =>
                     {
                
                         total.Stages |= current.Value.Stages;
                         return total;
                     })))
        {
            PushConstants.Add(pushConstant.Name,pushConstant);
        };


        PushConstantRanges = PushConstants.Select(c => new VkPushConstantRange()
        {
            offset = 0,
            size = (uint)c.Value.Size,
            stageFlags = c.Value.Stages
        }).ToArray();


        unsafe
        {
            fixed (VkDescriptorSetLayout* pLayouts = Layouts.OrderBy(c => c.Key).Select(c => c.Value).ToArray())
            {
                fixed (VkPushConstantRange* pRanges = PushConstantRanges)
                {
                    var pipelineLayoutInfo = new VkPipelineLayoutCreateInfo
                    {
                        sType = VkStructureType.VK_STRUCTURE_TYPE_PIPELINE_LAYOUT_CREATE_INFO,
                        pSetLayouts = pLayouts,
                        setLayoutCount = (uint)Layouts.Count,
                        pPushConstantRanges = pRanges,
                        pushConstantRangeCount = (uint)PushConstantRanges.Length
                    };

                    VkPipelineLayout newLayout;
                    vkCreatePipelineLayout(SGraphicsModule.Get().GetDevice(), &pipelineLayoutInfo, null, &newLayout);
                    PipelineLayout = newLayout;
                }
            }
        }
    }


    public void Bind(Frame frame)
    {
        {
            var cmd = frame.GetCommandBuffer();
            cmd.BindShaders(Shaders.Select((shader) => new Pair<VkShaderEXT, VkShaderStageFlags>(SGraphicsModule.Get()
                    .ShaderModuleToShader(Id, shader, Layouts.Select(c => c.Value), PushConstantRanges),
                shader.StageFlags)));
        }
    }
    protected override void OnDispose(bool isManual)
    {
        unsafe
        {
            var device = SGraphicsModule.Get().GetDevice();
            
            foreach (var (key, value) in Layouts)
            {
                vkDestroyDescriptorSetLayout(device,value,null);
            }
            
            vkDestroyPipelineLayout(device,PipelineLayout,null);
        }
    }
}
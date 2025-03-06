using System.Runtime.InteropServices;
using System.Text.Json;
using Rin.Engine.Core;
using Rin.Engine.Graphics.Descriptors;
using TerraFX.Interop.Vulkan;
using static TerraFX.Interop.Vulkan.Vulkan;

namespace Rin.Engine.Graphics.Shaders.Slang;

public class SlangGraphicsShader : IGraphicsShader
{
    private readonly Task _compileTask;
    private readonly Dictionary<uint, VkDescriptorSetLayout> _descriptorLayouts = [];

    private readonly string _filePath;

    private readonly List<Pair<VkShaderEXT, VkShaderStageFlags>> _shaders = [];
    private bool _hasFragment;
    private bool _hasVertex;
    private VkPipelineLayout _pipelineLayout;

    public SlangGraphicsShader(SlangShaderManager manager, string filePath)
    {
        _compileTask = manager.Compile(this);
        _filePath = filePath;
    }

    public void Dispose()
    {
        var device = SGraphicsModule.Get().GetDevice();
        unsafe
        {
            vkDestroyPipelineLayout(device, _pipelineLayout, null);
        }

        foreach (var (first, second) in _shaders) device.DestroyShader(first);
    }

    public Dictionary<string, Resource> Resources { get; } = [];
    public Dictionary<string, PushConstant> PushConstants { get; } = [];

    public bool Bind(VkCommandBuffer cmd, bool wait = false)
    {
        _compileTask.Wait();
        if (wait && !_compileTask.IsCompleted)
            _compileTask.Wait();
        else if (!_compileTask.IsCompleted) return false;

        cmd.BindShaders(_shaders);

        if (!_hasFragment || !_hasVertex)
        {
            List<VkShaderStageFlags> toUnbind = [];

            if (!_hasFragment) toUnbind.Add(VkShaderStageFlags.VK_SHADER_STAGE_FRAGMENT_BIT);

            if (!_hasVertex) toUnbind.Add(VkShaderStageFlags.VK_SHADER_STAGE_VERTEX_BIT);

            cmd.UnBindShaders(toUnbind);
        }

        return true;
    }

    public void Compile(ICompilationContext context)
    {
        if (context.Manager is SlangShaderManager manager)
        {
            var session = manager.GetSession();

            var fileUri = FileUri.FromSystemPath(_filePath);
            var fileData = SEngine.Get().FileSystem.ReadAllText(fileUri);
            using var module = session.LoadModuleFromSourceString(_filePath, _filePath, fileData);
            if (module == null) throw new ShaderCompileException("Failed to load slang shader module.");
            List<SlangEntryPoint> entryPoints = [];
            {
                var entryPoint = module.FindEntryPointByName("vertex");
                if (entryPoint != null)
                {
                    entryPoints.Add(entryPoint);
                    _hasVertex = true;
                }
            }

            {
                var entryPoint = module.FindEntryPointByName("fragment");
                if (entryPoint != null)
                {
                    entryPoints.Add(entryPoint);
                    _hasFragment = true;
                }
            }

            List<Pair<VkShaderStageFlags, SlangBlob>> code = [];
            // We could link them together, but we need min shader params
            foreach (var entryPoint in entryPoints)
            {
                using var composedProgram = session.CreateComposedProgram(module, [entryPoint]);

                if (composedProgram == null) throw new ShaderCompileException("Failed to create composed program.");

                using var linkedProgram = composedProgram.Link();

                if (linkedProgram == null) throw new ShaderCompileException("Failed to link composed program.");

                var generatedCode = linkedProgram.GetEntryPointCode(0, 0);

                if (generatedCode == null) throw new ShaderCompileException("Failed to generate code.");

                using var reflectionBlob = linkedProgram.ToLayoutJson();

                var jsonString = Marshal.PtrToStringUTF8(reflectionBlob.GetDataPointer());

                if (jsonString == null) throw new ShaderCompileException("Failed to get reflection data.");

                var reflectionData = JsonSerializer.Deserialize<ReflectionData>(jsonString);

                if (reflectionData == null) throw new ShaderCompileException("Failed to parse reflection data.");

                var entryPointStage = reflectionData.EntryPoints.FirstOrDefault()?.Stage == "vertex"
                    ? VkShaderStageFlags.VK_SHADER_STAGE_VERTEX_BIT
                    : VkShaderStageFlags.VK_SHADER_STAGE_FRAGMENT_BIT;

                code.Add(new Pair<VkShaderStageFlags, SlangBlob>(entryPointStage, generatedCode));

                var parameters = reflectionData.Parameters.ToList();

                foreach (var reflectionDataEntryPoint in reflectionData.EntryPoints)
                    parameters.AddRange(reflectionDataEntryPoint.Parameters);

                foreach (var parameter in parameters)
                {
                    var name = parameter.Name;
                    if (parameter.Binding is { Kind: "descriptorTableSlot" } binding)
                    {
                        var set = binding.Set ?? 0;
                        var index = binding.Binding ?? 0;
                        var count = parameter.Type.ElementCount ?? 1;
                        var stages = entryPointStage;
                        VkDescriptorBindingFlags bindingFlags = 0;
                        var bindingType = VkDescriptorType.VK_DESCRIPTOR_TYPE_COMBINED_IMAGE_SAMPLER;
                        foreach (var parameterAttribute in parameter.UserAttributes)
                            if (parameterAttribute.Name == "AllStages")
                            {
                                stages = VkShaderStageFlags.VK_SHADER_STAGE_ALL;
                            }
                            else if (parameterAttribute.Name == "UpdateAfterBind")
                            {
                                bindingFlags |= VkDescriptorBindingFlags.VK_DESCRIPTOR_BINDING_UPDATE_AFTER_BIND_BIT;
                            }
                            else if (parameterAttribute.Name == "Partial")
                            {
                                bindingFlags |= VkDescriptorBindingFlags.VK_DESCRIPTOR_BINDING_PARTIALLY_BOUND_BIT;
                            }
                            else if (parameterAttribute.Name == "Variable")
                            {
                                bindingFlags |= VkDescriptorBindingFlags
                                    .VK_DESCRIPTOR_BINDING_VARIABLE_DESCRIPTOR_COUNT_BIT;
                                parameterAttribute.Arguments.FirstOrDefault()?.TryGetValue(out count);
                            }

                        if (Resources.ContainsKey(name))
                        {
                            Resources[name].Stages |= stages;
                            Resources[name].BindingFlags |= bindingFlags;
                        }
                        else
                        {
                            var typeName = parameter.Type.BaseShape ?? parameter.Type.ElementType?.BaseShape ?? "";

                            if (typeName == "texture2D")
                                bindingType = VkDescriptorType.VK_DESCRIPTOR_TYPE_COMBINED_IMAGE_SAMPLER;

                            Resources.Add(name, new Resource
                            {
                                Binding = (uint)index,
                                BindingFlags = bindingFlags,
                                Count = (uint)count,
                                Name = name,
                                Set = (uint)set,
                                Stages = stages,
                                Type = bindingType
                            });
                        }
                    }
                    else if (parameter.Binding is { Kind: "pushConstantBuffer" } uniformBinding)
                    {
                        if (PushConstants.TryGetValue(name, out var constant))
                            constant.Stages |= entryPointStage;
                        else
                            PushConstants.Add(name, new PushConstant
                            {
                                Name = name,
                                Size = (uint)(parameter.Type.ElementVarLayout?.Binding?.Size ?? 0),
                                Stages = entryPointStage
                            });
                    }
                }
            }

            {
                List<VkPushConstantRange> pushConstantRanges = [];
                SortedDictionary<uint, DescriptorLayoutBuilder> builders = [];
                foreach (var (key, item) in Resources)
                {
                    if (!builders.ContainsKey(item.Set)) builders.Add(item.Set, new DescriptorLayoutBuilder());

                    builders[item.Set].AddBinding(item.Binding, item.Type, item.Stages, item.Count, item.BindingFlags);
                }

                foreach (var (key, item) in PushConstants)
                    pushConstantRanges.Add(new VkPushConstantRange
                    {
                        stageFlags = item.Stages,
                        offset = 0,
                        size = item.Size
                    });

                var max = builders.Count == 0 ? 0 : builders.Keys.Max();
                List<VkDescriptorSetLayout> layouts = [];

                for (uint i = 0; i < max + 1; i++)
                {
                    var newLayout = builders.TryGetValue(i, out var value)
                        ? value.Build()
                        : new DescriptorLayoutBuilder().Build();
                    _descriptorLayouts.Add(i, newLayout);
                    layouts.Add(newLayout);
                }

                var device = SGraphicsModule.Get().GetDevice();
                unsafe
                {
                    fixed (VkDescriptorSetLayout* pSetLayouts = layouts.ToArray())
                    fixed (VkPushConstantRange* pPushConstants = pushConstantRanges.ToArray())
                    fixed (byte* pName = "main"u8)
                    {
                        var setLayoutCount = (uint)layouts.Count;
                        var pushConstantRangeCount = (uint)pushConstantRanges.Count;
                        var pipelineLayoutCreateInfo = new VkPipelineLayoutCreateInfo
                        {
                            sType = VkStructureType.VK_STRUCTURE_TYPE_PIPELINE_LAYOUT_CREATE_INFO,
                            setLayoutCount = setLayoutCount,
                            pSetLayouts = pSetLayouts,
                            pushConstantRangeCount = pushConstantRangeCount,
                            pPushConstantRanges = pPushConstants
                        };

                        var pipelineLayout = new VkPipelineLayout();
                        vkCreatePipelineLayout(device, &pipelineLayoutCreateInfo, null, &pipelineLayout);
                        _pipelineLayout = pipelineLayout;

                        foreach (var (stage, blob) in code)
                        {
                            var shaderSize = blob.GetSize();
                            var createInfo = new VkShaderCreateInfoEXT
                            {
                                sType = VkStructureType.VK_STRUCTURE_TYPE_SHADER_CREATE_INFO_EXT,
                                stage = stage,
                                nextStage = stage == VkShaderStageFlags.VK_SHADER_STAGE_VERTEX_BIT
                                    ? VkShaderStageFlags.VK_SHADER_STAGE_FRAGMENT_BIT
                                    : 0,
                                codeType = VkShaderCodeTypeEXT.VK_SHADER_CODE_TYPE_SPIRV_EXT,
                                codeSize = (nuint)blob.GetSize(),
                                pCode = blob.GetDataPointer().ToPointer(),
                                pName = (sbyte*)pName,
                                pushConstantRangeCount = pushConstantRangeCount,
                                setLayoutCount = setLayoutCount,
                                pPushConstantRanges = pPushConstants,
                                pSetLayouts = pSetLayouts,
                                pSpecializationInfo = null,
                                pNext = null
                            };
                            var result = device.CreateShaders(createInfo).First();
                            _shaders.Add(new Pair<VkShaderEXT, VkShaderStageFlags>(result,
                                stage));

                            blob.Dispose();
                        }
                    }
                }
            }
        }
    }

    public Dictionary<uint, VkDescriptorSetLayout> GetDescriptorSetLayouts()
    {
        return _descriptorLayouts;
    }

    public VkPipelineLayout GetPipelineLayout()
    {
        return _pipelineLayout;
    }
}
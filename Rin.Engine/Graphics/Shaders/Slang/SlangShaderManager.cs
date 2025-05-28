using TerraFX.Interop.Vulkan;

namespace Rin.Engine.Graphics.Shaders.Slang;

public class SlangShaderManager : IShaderManager
{
    private readonly BackgroundTaskQueue _compileTasks = new();
    private readonly object _computeLock = new();
    private readonly Dictionary<string, SlangComputeShader> _computeShaders = [];
    private readonly object _graphicsLock = new();
    private readonly Dictionary<string, SlangGraphicsShader> _graphicsShaders = [];
    private readonly SlangSession _session;


    public SlangShaderManager()
    {
        using var builder = new SlangSessionBuilder();
        _session = builder
            .AddSearchPath(Path.GetDirectoryName(SGraphicsModule.ShadersDirectory) ?? "")
            .AddTargetSpirv().Build();
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _compileTasks.Dispose();
        foreach (var shader in _graphicsShaders.Values) shader.Dispose();
        foreach (var shader in _computeShaders.Values) shader.Dispose();
        _graphicsShaders.Clear();
        _computeShaders.Clear();
        _session.Dispose();
    }

    public Task Compile(IShader shader)
    {
        return _compileTasks.Enqueue(() => shader.Compile(new SlangCompilationContext(this)));
    }

    public IGraphicsShader MakeGraphics(string path)
    {
        lock (_graphicsLock)
        {
            var absPath = Path.GetFullPath(path);

            {
                if (_graphicsShaders.TryGetValue(absPath, out var shader)) return shader;
            }

            {
                var shader = new SlangGraphicsShader(this, path);
                _graphicsShaders.Add(absPath, shader);
                return shader;
            }
        }
    }

    public IComputeShader MakeCompute(string path)
    {
        lock (_computeLock)
        {
            var absPath = Path.GetFullPath(path);

            {
                if (_computeShaders.TryGetValue(absPath, out var shader)) return shader;
            }

            {
                var shader = new SlangComputeShader(this, path);
                _computeShaders.Add(absPath, shader);
                return shader;
            }
        }
    }


    public static void ReflectShader(ReflectionData reflectionData, Dictionary<string, Resource> resources,
        Dictionary<string, PushConstant> pushConstants, VkShaderStageFlags entryPointStage)
    {
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
                    switch (parameterAttribute.Name)
                    {
                        case "AllStages":
                            stages = VkShaderStageFlags.VK_SHADER_STAGE_ALL;
                            break;
                        case "UpdateAfterBind":
                            bindingFlags |= VkDescriptorBindingFlags.VK_DESCRIPTOR_BINDING_UPDATE_AFTER_BIND_BIT;
                            break;
                        case "Partial":
                            bindingFlags |= VkDescriptorBindingFlags.VK_DESCRIPTOR_BINDING_PARTIALLY_BOUND_BIT;
                            break;
                        case "Variable":
                            bindingFlags |= VkDescriptorBindingFlags
                                .VK_DESCRIPTOR_BINDING_VARIABLE_DESCRIPTOR_COUNT_BIT;
                            parameterAttribute.Arguments.FirstOrDefault()?.TryGetValue(out count);
                            break;
                        case "TextureBinding":
                            bindingType = VkDescriptorType.VK_DESCRIPTOR_TYPE_SAMPLED_IMAGE;
                            break;
                        case "StorageImageBinding":
                            bindingType = VkDescriptorType.VK_DESCRIPTOR_TYPE_STORAGE_IMAGE;
                            break;
                        case "SamplerBinding":
                            bindingType = VkDescriptorType.VK_DESCRIPTOR_TYPE_SAMPLER;
                            break;
                        case "UniformBufferBinding":
                            bindingType = VkDescriptorType.VK_DESCRIPTOR_TYPE_UNIFORM_BUFFER;
                            break;
                        case "StorageBufferBinding":
                            bindingType = VkDescriptorType.VK_DESCRIPTOR_TYPE_STORAGE_BUFFER;
                            break;
                    }

                if (resources.ContainsKey(name))
                {
                    resources[name].Stages |= stages;
                    resources[name].BindingFlags |= bindingFlags;
                }
                else
                {
                    resources.Add(name, new Resource
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
                if (pushConstants.TryGetValue(name, out var constant))
                    constant.Stages |= entryPointStage;
                else
                    pushConstants.Add(name, new PushConstant
                    {
                        Name = name,
                        Size = (uint)(parameter.Type.ElementVarLayout?.Binding?.Size ?? 0),
                        Stages = VkShaderStageFlags.VK_SHADER_STAGE_ALL_GRAPHICS |
                                 VkShaderStageFlags.VK_SHADER_STAGE_COMPUTE_BIT
                    });
            }
        }
    }


    public static IEnumerable<string> ImportFile(string filePath, HashSet<string> included)
    {
        using var dataStream = SEngine.Get().Sources.Read(filePath);
        using var reader = new StreamReader(dataStream);
        while (reader.ReadLine() is { } line)
            if (line.StartsWith("#include"))
            {
                var includeString = line[(line.IndexOf('"') + 1)..line.LastIndexOf('"')];
                if (included.Add(includeString))
                    foreach (var importedLine in ImportFile(includeString, included))
                        yield return importedLine;
            }
            else
            {
                yield return line;
            }
    }

    public SlangSession GetSession()
    {
        return _session;
    }
}
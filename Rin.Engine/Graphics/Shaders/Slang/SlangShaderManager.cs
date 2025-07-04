using TerraFX.Interop.Vulkan;

namespace Rin.Engine.Graphics.Shaders.Slang;

public class SlangShaderManager : IShaderManager
{
    private readonly BackgroundTaskQueue _compileTasks = new();
    private readonly Lock _computeLock = new();
    private readonly Dictionary<string, SlangComputeShader> _computeShaders = [];
    private readonly Lock _graphicsLock = new();
    private readonly Dictionary<string, SlangGraphicsShader> _graphicsShaders = [];
    private readonly SlangSession _session;


    public SlangShaderManager()
    {
        using var builder = new SlangSessionBuilder();
        _session = builder
            .AddTargetSpirv()
            .Build();
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
        Dictionary<string, PushConstant> pushConstants, ShaderStage entryPointStage)
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
                DescriptorBindingFlags bindingFlags = 0;
                var bindingType = DescriptorType.CombinedSamplerImage;
                foreach (var parameterAttribute in parameter.UserAttributes)
                    switch (parameterAttribute.Name)
                    {
                        case "AllStages":
                            stages = ShaderStage.All;
                            break;
                        case "UpdateAfterBind":
                            bindingFlags |= DescriptorBindingFlags.UpdateAfterBind;
                            break;
                        case "Partial":
                            bindingFlags |= DescriptorBindingFlags.PartiallyBound;
                            break;
                        case "Variable":
                            bindingFlags |= DescriptorBindingFlags.Variable;
                            parameterAttribute.Arguments.FirstOrDefault()?.TryGetValue(out count);
                            break;
                        case "TextureBinding":
                            bindingType = DescriptorType.CombinedSamplerImage;
                            break;
                        case "StorageImageBinding":
                            bindingType = DescriptorType.StorageImage;
                            break;
                        case "SamplerBinding":
                            bindingType = DescriptorType.Sampler;
                            break;
                        case "UniformBufferBinding":
                            bindingType = DescriptorType.UniformBuffer;
                            break;
                        case "StorageBufferBinding":
                            bindingType = DescriptorType.StorageBuffer;
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
                        Stages = ShaderStage.AllGraphics |
                                 ShaderStage.Compute
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
using Rin.Core.Graphics.Shaders;
using Rin.Core.Shared.Threading;
using Rin.Slang;

namespace Rin.Graphics.Vulkan.Shaders.Compiled;

public class CompiledShaderManager : IShaderManager
{
    private readonly BackgroundTaskQueue _compileTasks = new()
    {
        Name = "Rin.Slang Shader Load Queue"
    };

    private readonly Lock _computeLock = new();
    private readonly Dictionary<string, CompiledComputeShader> _computeShaders = [];
    private readonly Lock _graphicsLock = new();
    private readonly Dictionary<string, CompiledGraphicsShader> _graphicsShaders = [];

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _compileTasks.Dispose();
        foreach (var shader in _graphicsShaders.Values) shader.Dispose();
        foreach (var shader in _computeShaders.Values) shader.Dispose();
        _graphicsShaders.Clear();
        _computeShaders.Clear();
    }

    public Task Compile(IShader shader)
    {
        return _compileTasks.Enqueue(() => shader.Compile(new CompiledShaderCompilationContext(this)));
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
                var shader = new CompiledGraphicsShader(this, path);
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
                var shader = new CompiledComputeShader(this, path);
                _computeShaders.Add(absPath, shader);
                return shader;
            }
        }
    }

    public static void ReflectShader(SlangReflectionData reflectionData, Dictionary<string, Resource> resources,
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
                        case "SampledTextureBinding":
                            bindingType = DescriptorType.CombinedSamplerImage;
                            break;
                        case "TextureBinding" or "TextureArrayBinding" or "CubemapBinding":
                            bindingType = DescriptorType.SampledImage;
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
                        default:
                        {
                            if (parameterAttribute.Name.EndsWith("Binding"))
                                throw new ShaderCompileException(
                                    $"Unknown Shader Binding :{parameterAttribute.Name}");
                        }
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
            else if (parameter.Binding is { Kind: "pushConstantBuffer" })
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
}

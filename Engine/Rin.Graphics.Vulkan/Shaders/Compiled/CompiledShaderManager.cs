using Rin.Core.Graphics.Shaders;
using Rin.Graphics.Vulkan.Descriptors;
using TerraFX.Interop.Vulkan;
using Rin.Core.Shared.Threading;
using Rin.Shade;
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
        return MakeGraphics(path, null);
    }

    public IGraphicsShader MakeGraphics(IGraphicsDescriptor descriptor)
    {
        return MakeGraphics(descriptor.Path, descriptor);
    }

    public IComputeShader MakeCompute(string path)
    {
        return MakeCompute(path, null);
    }

    public IComputeShader MakeCompute(IComputeDescriptor descriptor)
    {
        return MakeCompute(descriptor.Path, descriptor);
    }

    private IGraphicsShader MakeGraphics(string path, IGraphicsDescriptor? descriptor)
    {
        lock (_graphicsLock)
        {
            var absPath = Path.GetFullPath(path);

            {
                if (_graphicsShaders.TryGetValue(absPath, out var shader)) return shader;
            }

            {
                var shader = new CompiledGraphicsShader(this, path, descriptor);
                _graphicsShaders.Add(absPath, shader);
                return shader;
            }
        }
    }

    private IComputeShader MakeCompute(string path, IComputeDescriptor? descriptor)
    {
        lock (_computeLock)
        {
            var absPath = Path.GetFullPath(path);

            {
                if (_computeShaders.TryGetValue(absPath, out var shader)) return shader;
            }

            {
                var shader = new CompiledComputeShader(this, path, descriptor);
                _computeShaders.Add(absPath, shader);
                return shader;
            }
        }
    }

    /// <summary>
    ///     Builds one descriptor set layout per set index (0 up to the highest set used), with any set
    ///     that holds a named bindless block taking the engine's registered layout instead of one built
    ///     from reflection. Bindless blocks must be set 0: the engine binds its global set once per
    ///     frame at that index, so a block anywhere else would never actually be bound.
    /// </summary>
    public static List<VkDescriptorSetLayout> BuildDescriptorLayouts(IEnumerable<Resource> resources,
        IReadOnlyDictionary<string, uint> bindlessBlocks)
    {
        var module = VulkanGraphicsModule.Get();
        var registered = new Dictionary<uint, VkDescriptorSetLayout>();
        foreach (var (name, set) in bindlessBlocks)
        {
            if (set != 0)
                throw new ShaderCompileException(
                    $"Bindless block '{name}' landed at set {set}, but the global bindless set is bound once per " +
                    "frame at set 0 - declare it before any other parameter block");

            registered[set] = module.FindBindlessBlockLayout(name) ??
                              throw new ShaderCompileException($"No bindless block is registered as '{name}'");
        }

        SortedDictionary<uint, DescriptorLayoutBuilder> builders = [];
        foreach (var item in resources)
        {
            if (registered.ContainsKey(item.Set))
                throw new ShaderCompileException(
                    $"Resource '{item.Name}' shares set {item.Set} with a bindless block");

            if (!builders.ContainsKey(item.Set)) builders.Add(item.Set, new DescriptorLayoutBuilder());
            builders[item.Set].AddBinding(item.Binding, item.Type, item.Stages, item.Count, item.BindingFlags);
        }

        var max = builders.Keys.Concat(registered.Keys).DefaultIfEmpty().Max();
        List<VkDescriptorSetLayout> layouts = [];
        for (uint i = 0; i < max + 1; i++)
            layouts.Add(registered.TryGetValue(i, out var registeredLayout) ? registeredLayout
                : builders.TryGetValue(i, out var builder) ? builder.Build()
                : new DescriptorLayoutBuilder().Build());
        return layouts;
    }

    public static void ReflectShader(SlangReflectionData reflectionData, Dictionary<string, Resource> resources,
        Dictionary<string, PushConstant> pushConstants, ShaderStage entryPointStage,
        Dictionary<string, uint>? bindlessBlocks = null)
    {
        var parameters = reflectionData.Parameters.ToList();

        foreach (var reflectionDataEntryPoint in reflectionData.EntryPoints)
            parameters.AddRange(reflectionDataEntryPoint.Parameters);

        void AddResource(string name, uint set, uint index, int count,
            SlangReflectionData.UserAttributeField[] userAttributes)
        {
            var stages = entryPointStage;
            DescriptorBindingFlags bindingFlags = 0;
            var bindingType = DescriptorType.CombinedSamplerImage;
            foreach (var parameterAttribute in userAttributes)
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

            if (resources.TryGetValue(name, out var existing))
            {
                existing.Stages |= stages;
                existing.BindingFlags |= bindingFlags;
            }
            else
            {
                resources.Add(name, new Resource
                {
                    Binding = index,
                    BindingFlags = bindingFlags,
                    Count = (uint)count,
                    Name = name,
                    Set = set,
                    Stages = stages,
                    Type = bindingType
                });
            }
        }

        foreach (var parameter in parameters)
        {
            var name = parameter.Name;
            if (parameter.Binding is { Kind: "descriptorTableSlot" } binding)
            {
                AddResource(name, (uint)(binding.Set ?? 0), (uint)(binding.Binding ?? 0),
                    parameter.Type.ElementCount ?? 1, parameter.UserAttributes);
            }
            else if (parameter.Type.Kind == "parameterBlock" &&
                     parameter.Binding is { Kind: "subElementRegisterSpace" } spaceBinding)
            {
                // A [BindingGroup] field lowers to a ParameterBlock<T>. Its own binding gives the SET
                // the whole block landed at - despite the "index" JSON key name, subElementRegisterSpace
                // has no separate "space" key, confirmed against real Slang -reflection-json output.
                // Each of T's own fields then carries its LOCAL binding within that set.
                var set = (uint)(spaceBinding.Binding ?? 0);

                // A named bindless block is served by the engine's own set, so its fields are not
                // resources this shader builds a layout for.
                if (parameter.UserAttributes.FirstOrDefault(a => a.Name == "BindlessBlock") is { } bindless)
                {
                    if (bindlessBlocks is null)
                        throw new ShaderCompileException(
                            $"'{parameter.Name}' is a bindless block, which this kind of shader can't bind");

                    bindlessBlocks[bindless.Arguments[0].GetValue<string>()] = set;
                    continue;
                }

                foreach (var field in parameter.Type.ElementType?.Fields ?? [])
                {
                    if (field.Binding is not { Kind: "descriptorTableSlot" } fieldBinding) continue;
                    AddResource(field.Name, set, (uint)(fieldBinding.Binding ?? 0),
                        field.Type.ElementCount ?? 1, field.UserAttributes);
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

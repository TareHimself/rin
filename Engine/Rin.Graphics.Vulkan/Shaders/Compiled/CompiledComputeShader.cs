using System.Collections.Frozen;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Rin.Core;
using Rin.Core.Graphics;
using Rin.Core.Graphics.Shaders;
using Rin.Graphics.Vulkan.Descriptors;
using Rin.Slang;
using TerraFX.Interop.Vulkan;
using static TerraFX.Interop.Vulkan.Vulkan;

namespace Rin.Graphics.Vulkan.Shaders.Compiled;

public class CompiledComputeShader : IComputeShader, IVulkanShader
{
    private readonly Task _compileTask;
    private readonly Dictionary<uint, VkDescriptorSetLayout> _descriptorLayouts = [];
    private readonly string _filePath;
    private VkPipeline _pipeline;
    private VkPipelineLayout _pipelineLayout;
    private VkShaderModule _shaderModule;

    public CompiledComputeShader(CompiledShaderManager manager, string filePath)
    {
        _compileTask = manager.Compile(this);
        _filePath = filePath;
    }

    public void Dispose()
    {
        var device = VulkanGraphicsModule.Get().GetDevice();
        unsafe
        {
            vkDestroyPipeline(device, _pipeline, null);
            vkDestroyShaderModule(device, _shaderModule, null);
            vkDestroyPipelineLayout(device, _pipelineLayout, null);
        }
    }

    public bool Ready => _compileTask.IsCompleted;

    public IComputeBindContext? Bind(IExecutionContext ctx, bool wait = true)
    {
        _compileTask.Wait();
        if (wait && !_compileTask.IsCompleted)
            _compileTask.Wait();
        else if (!_compileTask.IsCompleted) return null;

        Debug.Assert(ctx is VulkanExecutionContext);
        var vkContext = (VulkanExecutionContext)ctx;
        vkCmdBindPipeline(vkContext.CommandBuffer,
            VkPipelineBindPoint.VK_PIPELINE_BIND_POINT_COMPUTE, _pipeline);

        return new VulkanComputeBindContext(this, vkContext);
    }

    public void Compile(ICompilationContext context)
    {
        var crshPath = Path.ChangeExtension(_filePath, ".crsh");
        using var stream = Global.Sources.Read(crshPath);
        var compiledShader = ShaderPackageReader.Read(stream);

        if (compiledShader.Kind != ShaderKind.Compute)
            throw new ShaderCompileException($"'{_filePath}' is not a compute shader");

        var stage = compiledShader.Stages.FirstOrDefault() ??
                    throw new ShaderCompileException("Missing entry point");

        Debug.Assert(stage.Stage == "compute");

        var groupSize = compiledShader.ThreadGroupSize ??
                         throw new ShaderCompileException("Missing thread group size");
        GroupSizeX = groupSize[0];
        GroupSizeY = groupSize[1];
        GroupSizeZ = groupSize[2];
        const ShaderStage entryPointStage = ShaderStage.Compute;

        var resources = new Dictionary<string, Resource>();
        var pushConstants = new Dictionary<string, PushConstant>();
        CompiledShaderManager.ReflectShader(stage.Reflection, resources, pushConstants, entryPointStage);
        Resources = resources.ToFrozenDictionary();
        PushConstants = pushConstants.ToFrozenDictionary();
        {
            SortedDictionary<uint, DescriptorLayoutBuilder> builders = [];
            foreach (var item in Resources.Values)
            {
                if (!builders.ContainsKey(item.Set)) builders.Add(item.Set, new DescriptorLayoutBuilder());

                builders[item.Set].AddBinding(item.Binding, item.Type, item.Stages, item.Count, item.BindingFlags);
            }

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

            var device = VulkanGraphicsModule.Get().GetDevice();

            _pipelineLayout = device.CreatePipelineLayout(CollectionsMarshal.AsSpan(layouts));
            _shaderModule = device.CreateShaderModule(stage.Spirv);
            _pipeline = device.CreateComputePipeline(_pipelineLayout, _shaderModule);
        }
    }

    public uint GroupSizeX { get; private set; }
    public uint GroupSizeY { get; private set; }
    public uint GroupSizeZ { get; private set; }

    public FrozenDictionary<string, Resource> Resources { get; set; } = FrozenDictionary<string, Resource>.Empty;

    public FrozenDictionary<string, PushConstant> PushConstants { get; set; } =
        FrozenDictionary<string, PushConstant>.Empty;

    public Dictionary<uint, VkDescriptorSetLayout> GetDescriptorSetLayouts()
    {
        return _descriptorLayouts;
    }

    public VkPipelineBindPoint GetBindPoint()
    {
        return VkPipelineBindPoint.VK_PIPELINE_BIND_POINT_COMPUTE;
    }

    public VkPipelineLayout GetPipelineLayout()
    {
        return _pipelineLayout;
    }

    public void Dispatch(in VkCommandBuffer cmd, uint x, uint y = 1, uint z = 1)
    {
        Debug.Assert(x != 0 && y != 0 && z != 0);
        vkCmdDispatch(cmd, x, y, z);
    }
}

using System.Collections.Frozen;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using Rin.Framework.Graphics.Shaders;
using Rin.Framework.Graphics.Vulkan.Descriptors;
using TerraFX.Interop.Vulkan;
using static TerraFX.Interop.Vulkan.Vulkan;

namespace Rin.Framework.Graphics.Vulkan.Shaders.Slang;

public class SlangComputeShader : IComputeShader, IVulkanShader
{
    private readonly Task _compileTask;
    private readonly Dictionary<uint, VkDescriptorSetLayout> _descriptorLayouts = [];
    private readonly string _filePath;
    private VkPipeline _pipeline;
    private VkPipelineLayout _pipelineLayout;
    private VkShaderModule _shaderModule;

    public SlangComputeShader(SlangShaderManager manager, string filePath)
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
        if (context.Manager is SlangShaderManager manager)
        {
            var includesProcessed = new HashSet<string>();
            var session = manager.GetSession();
            var fileData =
                string.Join('\n', SlangShaderManager.ImportFile(_filePath, includesProcessed)); //reader.ReadToEnd();
            var diag = new SlangBlob();
            var id = $"compute-{Guid.NewGuid().ToString()}.slang";
            using var module = session.LoadModuleFromSourceString(_filePath, _filePath, fileData, diag);
            if (module == null)
            {
                var str = Marshal.PtrToStringAnsi(diag.GetDataPointer()) ?? string.Empty;
                throw new ShaderCompileException("Failed to load slang shader module:\n" + str);
            }

            var entryPoint = module.FindEntryPointByName("compute") ??
                             throw new ShaderCompileException("Failed to find entrypoint");
            using var composedProgram = session.CreateComposedProgram(module, [entryPoint]);

            if (composedProgram == null) throw new ShaderCompileException("Failed to create composed program.");

            using var linkedProgram = composedProgram.Link();

            if (linkedProgram == null) throw new ShaderCompileException("Failed to link composed program.");

            diag.Dispose();
            diag = new SlangBlob();

            var codeBlob = linkedProgram.GetEntryPointCode(0, 0, ref diag);

            if (codeBlob == null)
            {
                var msg = diag.GetString();
                var length = diag.GetSize();
                throw new ShaderCompileException("Failed to generate code.\n" + msg);
            }

            using var reflectionBlob = linkedProgram.ToLayoutJson();

            var jsonString = Marshal.PtrToStringUTF8(reflectionBlob.GetDataPointer());

            if (jsonString == null) throw new ShaderCompileException("Failed to get reflection data.");

            var reflectionData = JsonSerializer.Deserialize<ReflectionData>(jsonString);

            if (reflectionData == null) throw new ShaderCompileException("Failed to parse reflection data.");

            var entryPointReflection = reflectionData.EntryPoints.FirstOrDefault() ??
                                       throw new ShaderCompileException("Missing entry point");

            Debug.Assert(entryPointReflection.Stage == "compute");

            var groupSize = entryPointReflection.ThreadGroupSize;
            GroupSizeX = groupSize[0];
            GroupSizeY = groupSize[1];
            GroupSizeZ = groupSize[2];
            const ShaderStage entryPointStage = ShaderStage.Compute;


            var resources = new Dictionary<string, Resource>();
            var pushConstants = new Dictionary<string, PushConstant>();
            SlangShaderManager.ReflectShader(reflectionData, resources, pushConstants, entryPointStage);
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

                _pipelineLayout = device.CreatePipelineLayout(layouts);
                _shaderModule = device.CreateShaderModule(codeBlob.AsReadOnlySpan());
                _pipeline = device.CreateComputePipeline(_pipelineLayout, _shaderModule);
                codeBlob.Dispose();
            }
        }
    }

    public uint GroupSizeX { get; private set; }
    public uint GroupSizeY { get; private set; }
    public uint GroupSizeZ { get; private set; }

    public FrozenDictionary<string, Resource> Resources { get; set; } = FrozenDictionary<string, Resource>.Empty;
    public FrozenDictionary<string, PushConstant> PushConstants { get; set; } = FrozenDictionary<string, PushConstant>.Empty;

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
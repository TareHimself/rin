using System.Collections.Frozen;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using Rin.Framework.Graphics.Shaders;
using Rin.Framework.Graphics.Vulkan.Descriptors;
using TerraFX.Interop.Vulkan;
using static TerraFX.Interop.Vulkan.Vulkan;

namespace Rin.Framework.Graphics.Vulkan.Shaders.Slang;

public class SlangGraphicsShader : IGraphicsShader, IVulkanShader
{
    private readonly Task _compileTask;
    private readonly Dictionary<uint, VkDescriptorSetLayout> _descriptorLayouts = [];

    private readonly string _filePath;
    private readonly List<Pair<VkShaderModule, ShaderStage>> _shaders = [];
    private VkPipeline _pipeline;
    private VkPipelineLayout _pipelineLayout;
    private VkShaderStageFlags _shaderStageFlags = 0;

    public SlangGraphicsShader(SlangShaderManager manager, string filePath)
    {
        _compileTask = manager.Compile(this);
        _filePath = filePath;
    }

    public void Dispose()
    {
        var device = VulkanGraphicsModule.Get().GetDevice();
        unsafe
        {
            vkDestroyPipelineLayout(device, _pipelineLayout, null);
            foreach (var (first, second) in _shaders) vkDestroyShaderModule(device, first, null);
            vkDestroyPipeline(device, _pipeline, null);
        }
    }

    public bool Ready => _compileTask.IsCompleted;

    public IGraphicsBindContext? Bind(IExecutionContext ctx, bool wait = true)
    {
        if (wait && !_compileTask.IsCompleted)
            _compileTask.Wait();
        else if (!_compileTask.IsCompleted) return null;

        Debug.Assert(ctx is VulkanExecutionContext);
        var vkContext = (VulkanExecutionContext)ctx;
        vkCmdBindPipeline(vkContext.CommandBuffer,
            VkPipelineBindPoint.VK_PIPELINE_BIND_POINT_GRAPHICS, _pipeline);

        return new VulkanGraphicsBindContext(this, vkContext);
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
            var id = $"graphics-{Guid.NewGuid().ToString()}.slang";
            using var module = session.LoadModuleFromSourceString(_filePath, _filePath, fileData, diag);
            if (module == null)
            {
                var str = Marshal.PtrToStringAnsi(diag.GetDataPointer()) ?? string.Empty;
                throw new ShaderCompileException("Failed to load slang shader module:\n" + str);
            }

            List<SlangEntryPoint> entryPoints = [];
            {
                var entryPoint = module.FindEntryPointByName("vertex");
                if (entryPoint != null)
                {
                    entryPoints.Add(entryPoint);
                }
            }

            {
                var entryPoint = module.FindEntryPointByName("fragment");
                if (entryPoint != null)
                {
                    entryPoints.Add(entryPoint);
                }
            }
            var resources = new Dictionary<string, Resource>();
            var pushConstants = new Dictionary<string, PushConstant>();
            List<Pair<ShaderStage, SlangBlob>> code = [];
            // We could link them together, but we need min shader params
            foreach (var entryPoint in entryPoints)
            {
                using var composedProgram = session.CreateComposedProgram(module, [entryPoint]);

                if (composedProgram == null) throw new ShaderCompileException("Failed to create composed program.");

                using var linkedProgram = composedProgram.Link();

                if (linkedProgram == null) throw new ShaderCompileException("Failed to link composed program.");

                diag.Dispose();
                diag = new SlangBlob();

                var generatedCode = linkedProgram.GetEntryPointCode(0, 0, ref diag);

                if (generatedCode == null)
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

                var entryPointStage = reflectionData.EntryPoints.FirstOrDefault()?.Stage == "vertex"
                    ? ShaderStage.Vertex
                    : ShaderStage.Fragment;

                code.Add(new Pair<ShaderStage, SlangBlob>(entryPointStage, generatedCode));

                if (reflectionData.EntryPoints.FirstOrDefault() is { } reflectionEntryPoint)
                {
                    if (reflectionEntryPoint is { Name: "fragment", Result: not null })
                        switch (reflectionEntryPoint.Result.Type.Kind)
                        {
                            case "struct":
                                AttachmentFormats = reflectionEntryPoint.Result.Type.Fields
                                    .Where(c => c.SemanticName == "SV_TARGET").Select(c =>
                                    {
                                        if (c.UserAttributes.FirstOrDefault(c => c.Name == "Attachment") is
                                            { } targetAttribute)
                                            return (ImageFormat)targetAttribute.Arguments[0].GetValue<int>();
                                        throw new Exception("Output parameter missing Attachment Attribute");
                                    }).ToArray();
                                break;
                            case "vector":
                            {
                                if (reflectionEntryPoint.UserAttributes.FirstOrDefault(c => c.Name == "Attachment") is
                                    { } targetAttribute)
                                    AttachmentFormats = [(ImageFormat)targetAttribute.Arguments[0].GetValue<int>()];
                            }
                                break;
                            default:
                                Debug.Fail("Unknown Result From Fragment Shader");
                                break;
                        }

                    foreach (var userAttributeField in reflectionEntryPoint.UserAttributes)
                        switch (userAttributeField.Name)
                        {
                            case "Depth":
                                UsesDepth = true;
                                break;
                            case "Stencil":
                                UsesStencil = true;
                                break;
                            case "BlendNone":
                                BlendMode = BlendMode.None;
                                break;
                            case "BlendUI":
                                BlendMode = BlendMode.UI;
                                break;
                            case "BlendOpaque":
                                BlendMode = BlendMode.Opaque;
                                break;
                            case "BlendTranslucent":
                                BlendMode = BlendMode.Translucent;
                                break;
                        }
                }

                SlangShaderManager.ReflectShader(reflectionData, resources, pushConstants, entryPointStage);
            }

            Resources = resources.ToFrozenDictionary();
            PushConstants = pushConstants.ToFrozenDictionary();

            {
                SortedDictionary<uint, DescriptorLayoutBuilder> builders = [];
                foreach (var (key, item) in Resources)
                {
                    if (!builders.ContainsKey(item.Set))
                        builders.Add(item.Set,new DescriptorLayoutBuilder());

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
                foreach (var (stage, blob) in code)
                {
                    var shaderModule = device.CreateShaderModule(blob.AsReadOnlySpan());
                    _shaders.Add(new Pair<VkShaderModule, ShaderStage>(shaderModule,
                        stage));
                    _shaderStageFlags |= stage.ToVk();
                    blob.Dispose();
                }

                _pipeline = device.CreateGraphicsPipeline(_pipelineLayout, AttachmentFormats, BlendMode, _shaders,
                    UsesDepth, UsesStencil);
            }
        }
    }

    public ImageFormat[] AttachmentFormats { get; set; } = [];
    public BlendMode BlendMode { get; set; } = BlendMode.None;
    public bool UsesStencil { get; set; }
    public bool UsesDepth { get; set; }

    public FrozenDictionary<string, Resource> Resources { get; set; } = FrozenDictionary<string, Resource>.Empty;
    public FrozenDictionary<string, PushConstant> PushConstants { get; set; } = FrozenDictionary<string, PushConstant>.Empty;

    public Dictionary<uint, VkDescriptorSetLayout> GetDescriptorSetLayouts()
    {
        return _descriptorLayouts;
    }

    public VkPipelineBindPoint GetBindPoint()
    {
        return VkPipelineBindPoint.VK_PIPELINE_BIND_POINT_GRAPHICS;
    }

    public VkPipelineLayout GetPipelineLayout()
    {
        return _pipelineLayout;
    }
}
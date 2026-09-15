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

public class CompiledGraphicsShader : IGraphicsShader, IVulkanShader
{
    private readonly Task _compileTask;
    private readonly Dictionary<uint, VkDescriptorSetLayout> _descriptorLayouts = [];

    private readonly string _filePath;
    private readonly List<Pair<VkShaderModule, ShaderStage>> _shaders = [];
    private VkPipeline _pipeline;
    private VkPipelineLayout _pipelineLayout;
    private VkShaderStageFlags _shaderStageFlags = 0;

    public CompiledGraphicsShader(CompiledShaderManager manager, string filePath)
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
        var crshPath = Path.ChangeExtension(_filePath, ".crsh");
        using var stream = Global.Sources.Read(crshPath);
        var compiledShader = ShaderPackageReader.Read(stream);

        if (compiledShader.Kind != ShaderKind.Graphics)
            throw new ShaderCompileException($"'{_filePath}' is not a graphics shader");

        var resources = new Dictionary<string, Resource>();
        var pushConstants = new Dictionary<string, PushConstant>();
        List<Pair<ShaderStage, byte[]>> code = [];

        foreach (var stage in compiledShader.Stages)
        {
            var entryPointStage = stage.Stage == "vertex" ? ShaderStage.Vertex : ShaderStage.Fragment;
            code.Add(new Pair<ShaderStage, byte[]>(entryPointStage, stage.Spirv));

            if (stage.Reflection.EntryPoints.FirstOrDefault() is { } reflectionEntryPoint)
            {
                if (reflectionEntryPoint is { Name: "fragment", Result: not null })
                    switch (reflectionEntryPoint.Result.Type.Kind)
                    {
                        case "struct":
                            AttachmentFormats = reflectionEntryPoint.Result.Type.Fields
                                .Where(c => c.SemanticName == "SV_TARGET").Select(c =>
                                {
                                    if (c.UserAttributes.FirstOrDefault(field => field.Name == "Attachment") is
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

            CompiledShaderManager.ReflectShader(stage.Reflection, resources, pushConstants, entryPointStage);
        }

        Resources = resources.ToFrozenDictionary();
        PushConstants = pushConstants.ToFrozenDictionary();

        {
            SortedDictionary<uint, DescriptorLayoutBuilder> builders = [];
            foreach (var (key, item) in Resources)
            {
                if (!builders.ContainsKey(item.Set))
                    builders.Add(item.Set, new DescriptorLayoutBuilder());

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
            foreach (var (stage, spirv) in code)
            {
                var shaderModule = device.CreateShaderModule(spirv);
                _shaders.Add(new Pair<VkShaderModule, ShaderStage>(shaderModule,
                    stage));
                _shaderStageFlags |= stage.ToVk();
            }

            _pipeline = device.CreateGraphicsPipeline(_pipelineLayout, AttachmentFormats, BlendMode,
                CollectionsMarshal.AsSpan(_shaders),
                UsesDepth, UsesStencil);
        }
    }

    public ImageFormat[] AttachmentFormats { get; set; } = [];
    public BlendMode BlendMode { get; set; } = BlendMode.None;
    public bool UsesStencil { get; set; }
    public bool UsesDepth { get; set; }

    public FrozenDictionary<string, Resource> Resources { get; set; } = FrozenDictionary<string, Resource>.Empty;

    public FrozenDictionary<string, PushConstant> PushConstants { get; set; } =
        FrozenDictionary<string, PushConstant>.Empty;

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

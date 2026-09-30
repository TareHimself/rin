using System.Collections.Frozen;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Rin.Core;
using Rin.Core.Graphics;
using Rin.Core.Graphics.Shaders;
using Rin.Graphics.Vulkan.Descriptors;
using Rin.Shade;
using Rin.Slang;
using TerraFX.Interop.Vulkan;
using static TerraFX.Interop.Vulkan.Vulkan;

namespace Rin.Graphics.Vulkan.Shaders.Compiled;

public class CompiledGraphicsShader : IGraphicsShader, IVulkanShader
{
    private readonly Task _compileTask;
    private readonly Dictionary<uint, VkDescriptorSetLayout> _descriptorLayouts = [];

    private readonly IGraphicsDescriptor? _descriptor;
    private readonly string _filePath;
    private readonly List<Pair<VkShaderModule, ShaderStage>> _shaders = [];
    private VkPipeline _pipeline;
    private VkPipelineLayout _pipelineLayout;
    private VkShaderStageFlags _shaderStageFlags = 0;

    public CompiledGraphicsShader(CompiledShaderManager manager, string filePath,
        IGraphicsDescriptor? descriptor = null)
    {
        _filePath = filePath;
        _descriptor = descriptor;
        _compileTask = manager.Compile(this);
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
        var bindlessBlocks = new Dictionary<string, uint>();
        List<Pair<ShaderStage, byte[]>> code = [];

        if (_descriptor is { } descriptor)
        {
            AttachmentFormats = descriptor.AttachmentFormats.Select(format => (ImageFormat)(int)format).ToArray();
            BlendState = descriptor.BlendState;
            UsesDepth = descriptor.UsesDepth;
            UsesStencil = descriptor.UsesStencil;
        }

        foreach (var stage in compiledShader.Stages)
        {
            var entryPointStage = stage.Stage == "vertex" ? ShaderStage.Vertex : ShaderStage.Fragment;
            code.Add(new Pair<ShaderStage, byte[]>(entryPointStage, stage.Spirv));

            if (_descriptor is null && stage.Reflection.EntryPoints.FirstOrDefault() is { } reflectionEntryPoint)
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
                            BlendState = BlendState.None;
                            break;
                        case "BlendUI":
                            BlendState = BlendState.Alpha;
                            break;
                        case "BlendOpaque":
                            BlendState = BlendState.Opaque;
                            break;
                        case "BlendTranslucent":
                            throw new NotImplementedException();
                    }
            }

            CompiledShaderManager.ReflectShader(stage.Reflection, resources, pushConstants, entryPointStage,
                bindlessBlocks);
        }

        Resources = resources.ToFrozenDictionary();
        PushConstants = pushConstants.ToFrozenDictionary();

        {
            var layouts = CompiledShaderManager.BuildDescriptorLayouts(Resources.Values, bindlessBlocks);
            for (var i = 0; i < layouts.Count; i++) _descriptorLayouts.Add((uint)i, layouts[i]);

            var device = VulkanGraphicsModule.Get().GetDevice();
            _pipelineLayout = device.CreatePipelineLayout(CollectionsMarshal.AsSpan(layouts));
            foreach (var (stage, spirv) in code)
            {
                var shaderModule = device.CreateShaderModule(spirv);
                _shaders.Add(new Pair<VkShaderModule, ShaderStage>(shaderModule,
                    stage));
                _shaderStageFlags |= stage.ToVk();
            }

            _pipeline = device.CreateGraphicsPipeline(_pipelineLayout, AttachmentFormats, BlendState,
                CollectionsMarshal.AsSpan(_shaders),
                UsesDepth, UsesStencil);
        }
    }

    public ImageFormat[] AttachmentFormats { get; set; } = [];
    public BlendState BlendState { get; set; } = BlendState.None;
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

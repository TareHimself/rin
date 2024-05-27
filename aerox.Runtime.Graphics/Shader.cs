using System.Text.Json.Nodes;
using shaderc;
using TerraFX.Interop.Vulkan;
using static TerraFX.Interop.Vulkan.Vulkan;

namespace aerox.Runtime.Graphics;

/// <summary>
///     Information about a <see cref="DeviceImage" /> resource for a shader
/// </summary>
public struct ShaderResourceImage
{
    public readonly uint Binding;
    public readonly string Name = "";
    public readonly uint Set;
    public readonly uint Count;
    public VkShaderStageFlags Flags;

    public ShaderResourceImage(JsonObject obj, VkShaderStageFlags inFlags)
    {
        obj["binding"]?.AsValue().TryGetValue(out Binding);
        obj["name"]?.AsValue().TryGetValue(out Name);
        obj["set"]?.AsValue().TryGetValue(out Set);
        obj["count"]?.AsValue().TryGetValue(out Count);
        Flags = inFlags;
    }
}

/// <summary>
///     Information about a push constant resource for a shader
/// </summary>
public struct ShaderResourcePushConstant
{
    public readonly string Name = "";
    public readonly uint Size;
    public readonly uint Offset;
    public VkShaderStageFlags Flags;

    public ShaderResourcePushConstant(JsonObject obj, VkShaderStageFlags inFlags)
    {
        obj["name"]?.AsValue().TryGetValue(out Name);
        obj["size"]?.AsValue().TryGetValue(out Size);
        obj["offset"]?.AsValue().TryGetValue(out Offset);
        Flags = inFlags;
    }
}

/// <summary>
///     Information about a <see cref="DeviceBuffer" /> resource for a shader
/// </summary>
public struct ShaderResourceBuffer
{
    public readonly uint Binding;
    public readonly string Name = "";
    public readonly uint Set;
    public readonly uint Count;
    public VkShaderStageFlags Flags;

    public ShaderResourceBuffer(JsonObject obj, VkShaderStageFlags inFlags)
    {
        obj["binding"]?.AsValue().TryGetValue(out Binding);
        obj["name"]?.AsValue().TryGetValue(out Name);
        obj["set"]?.AsValue().TryGetValue(out Set);
        obj["count"]?.AsValue().TryGetValue(out Count);
        Flags = inFlags;
    }
}

/// <summary>
///     Stores the reflected resources for a <see cref="Shader" />
/// </summary>
public struct ShaderResources
{
    public readonly Dictionary<string, ShaderResourceImage> Images = new();
    public readonly Dictionary<string, ShaderResourcePushConstant> PushConstants = new();
    public readonly Dictionary<string, ShaderResourceBuffer> Buffers = new();

    public ShaderResources()
    {
    }

    public ShaderResources(JsonObject data, VkShaderStageFlags inFlags)
    {
        var arr = data["images"]?.AsArray();

        if (arr != null)
            foreach (var asVal in arr)
            {
                var asObj = asVal?.AsObject();
                if (asObj == null) continue;
                var item = new ShaderResourceImage(asObj, inFlags);
                Images.Add(item.Name, item);
            }

        arr = data["pushConstants"]?.AsArray();

        if (arr != null)
            foreach (var asVal in arr)
            {
                var asObj = asVal?.AsObject();
                if (asObj == null) continue;
                var item = new ShaderResourcePushConstant(asObj, inFlags);
                PushConstants.Add(item.Name, item);
            }

        arr = data["buffers"]?.AsArray();

        if (arr == null) return;
        {
            foreach (var asVal in arr)
            {
                var asObj = asVal?.AsObject();
                if (asObj == null) continue;
                var item = new ShaderResourceBuffer(asObj, inFlags);
                Buffers.Add(item.Name, item);
            }
        }
    }
}

/// <summary>
///     A shader
/// </summary>
public class Shader : MultiDisposable
{
    private readonly VkShaderModule _module;
    private readonly VkShaderStageFlags _stageFlags = 0;
    public ShaderResources Resources = new();

    public Shader(VkShaderModule module, string sourceFile, string reflectionData)
    {
        
        _module = module;
        if (sourceFile.EndsWith(".vert"))
            _stageFlags |= VkShaderStageFlags.VK_SHADER_STAGE_VERTEX_BIT;
        else if (sourceFile.EndsWith(".frag"))
            _stageFlags |= VkShaderStageFlags.VK_SHADER_STAGE_FRAGMENT_BIT;
        else if (sourceFile.EndsWith(".comp")) _stageFlags |= VkShaderStageFlags.VK_SHADER_STAGE_COMPUTE_BIT;

        var jsonData = JsonNode.Parse(reflectionData)?.AsObject();

        if (jsonData != null) Resources = new ShaderResources(jsonData, _stageFlags);
    }

    public Shader(VkShaderModule module, VkShaderStageFlags stageFlags)
    {
        _module = module;
        _stageFlags = stageFlags;
    }

    public static ShaderKind GetShaderStage(string filePath)
    {
        if (filePath.EndsWith(".vert"))
            return ShaderKind.GlslVertexShader;
        if (filePath.EndsWith(".frag"))
            return ShaderKind.GlslFragmentShader;
        if (filePath.EndsWith(".comp")) return ShaderKind.GlslComputeShader;

        return ShaderKind.GlslVertexShader;
    }

    public VkShaderStageFlags GetStageFlags()
    {
        return _stageFlags;
    }

    public static implicit operator VkShaderModule(Shader s)
    {
        return s._module;
    }

    protected override void OnDispose(bool isManual)
    {
        var subsystem = SRuntime.Get().GetModule<SGraphicsModule>();
        unsafe
        {
            vkDestroyShaderModule(subsystem.GetDevice(), _module, null);
        }
    }
}
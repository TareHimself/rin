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
    public readonly uint binding;
    public readonly string name = "";
    public readonly uint set;
    public readonly uint count;
    public VkShaderStageFlags flags;

    public ShaderResourceImage(JsonObject obj, VkShaderStageFlags inFlags)
    {
        obj["binding"]?.AsValue().TryGetValue(out binding);
        obj["name"]?.AsValue().TryGetValue(out name);
        obj["set"]?.AsValue().TryGetValue(out set);
        obj["count"]?.AsValue().TryGetValue(out count);
        flags = inFlags;
    }
}

/// <summary>
///     Information about a push constant resource for a shader
/// </summary>
public struct ShaderResourcePushConstant
{
    public readonly string name = "";
    public readonly uint size;
    public readonly uint offset;
    public VkShaderStageFlags flags;

    public ShaderResourcePushConstant(JsonObject obj, VkShaderStageFlags inFlags)
    {
        obj["name"]?.AsValue().TryGetValue(out name);
        obj["size"]?.AsValue().TryGetValue(out size);
        obj["offset"]?.AsValue().TryGetValue(out offset);
        flags = inFlags;
    }
}

/// <summary>
///     Information about a <see cref="DeviceBuffer" /> resource for a shader
/// </summary>
public struct ShaderResourceBuffer
{
    public readonly uint binding;
    public readonly string name = "";
    public readonly uint set;
    public readonly uint count;
    public VkShaderStageFlags flags;

    public ShaderResourceBuffer(JsonObject obj, VkShaderStageFlags inFlags)
    {
        obj["binding"]?.AsValue().TryGetValue(out binding);
        obj["name"]?.AsValue().TryGetValue(out name);
        obj["set"]?.AsValue().TryGetValue(out set);
        obj["count"]?.AsValue().TryGetValue(out count);
        flags = inFlags;
    }
}

/// <summary>
///     Stores the reflected resources for a <see cref="Shader" />
/// </summary>
public struct ShaderResources
{
    public readonly Dictionary<string, ShaderResourceImage> images = new();
    public readonly Dictionary<string, ShaderResourcePushConstant> pushConstants = new();
    public readonly Dictionary<string, ShaderResourceBuffer> buffers = new();

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
                images.Add(item.name, item);
            }

        arr = data["pushConstants"]?.AsArray();

        if (arr != null)
            foreach (var asVal in arr)
            {
                var asObj = asVal?.AsObject();
                if (asObj == null) continue;
                var item = new ShaderResourcePushConstant(asObj, inFlags);
                pushConstants.Add(item.name, item);
            }

        arr = data["buffers"]?.AsArray();

        if (arr == null) return;
        {
            foreach (var asVal in arr)
            {
                var asObj = asVal?.AsObject();
                if (asObj == null) continue;
                var item = new ShaderResourceBuffer(asObj, inFlags);
                buffers.Add(item.name, item);
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
    public ShaderResources resources = new();

    public Shader(VkShaderModule module, string sourceFile, string reflectionData)
    {
        _module = module;
        if (sourceFile.EndsWith(".vert"))
            _stageFlags |= VkShaderStageFlags.VK_SHADER_STAGE_VERTEX_BIT;
        else if (sourceFile.EndsWith(".frag"))
            _stageFlags |= VkShaderStageFlags.VK_SHADER_STAGE_FRAGMENT_BIT;
        else if (sourceFile.EndsWith(".comp")) _stageFlags |= VkShaderStageFlags.VK_SHADER_STAGE_COMPUTE_BIT;

        var jsonData = JsonNode.Parse(reflectionData)?.AsObject();

        if (jsonData != null) resources = new ShaderResources(jsonData, _stageFlags);
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
        var subsystem = Runtime.Instance.GetModule<GraphicsModule>();
        unsafe
        {
            vkDestroyShaderModule(subsystem.GetDevice(), _module, null);
        }
    }
}
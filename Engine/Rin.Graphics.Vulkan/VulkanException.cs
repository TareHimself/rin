using TerraFX.Interop.Vulkan;

namespace Rin.Graphics.Vulkan;

/// <summary>
///     A Vulkan call made through TerraFX returned an error.
/// </summary>
public sealed class VulkanException(VkResult result, string call) : Exception($"{call} failed with {result}")
{
    public VkResult Result { get; } = result;
    public string Call { get; } = call;
}

public static class VkResultExtensions
{
    // Vulkan error codes are negative, success codes such as VK_SUBOPTIMAL_KHR and VK_INCOMPLETE are not.
    public static void ThrowIfFailed(this VkResult result, string call)
    {
        if ((int)result < 0) throw new VulkanException(result, call);
    }
}

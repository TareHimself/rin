using TerraFX.Interop.Vulkan;

namespace Rin.Engine.Graphics;

public record struct SamplerSpec
{
    public required ImageFilter Filter;
    public required ImageTiling Tiling;

    public static implicit operator VkSampler(SamplerSpec spec)
    {
        return SGraphicsModule.Get().GetSampler(spec);
    }
}
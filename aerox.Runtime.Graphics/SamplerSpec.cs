using TerraFX.Interop.Vulkan;

namespace aerox.Runtime.Graphics;

public struct SamplerSpec
{
    public EImageTiling Tiling;
    public EImageFilter Filter;

    public static implicit operator VkSampler(SamplerSpec spec)
    {
        return SGraphicsModule.Get().GetSampler(spec);
    }
}
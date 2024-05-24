using TerraFX.Interop.Vulkan;

namespace aerox.Runtime.Graphics;

public struct SamplerSpec
{
    public ImageTiling Tiling;
    public ImageFilter Filter;

    public static implicit operator VkSampler(SamplerSpec spec) => GraphicsModule.Get().GetSampler(spec);
}
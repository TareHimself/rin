using Rin.Core.Graphics;

namespace Rin.Graphics.Vulkan.Descriptors;

public struct SamplerWrite(SamplerSpec spec)
{
    public SamplerSpec Sampler = spec;
    public uint Index = 0;
}
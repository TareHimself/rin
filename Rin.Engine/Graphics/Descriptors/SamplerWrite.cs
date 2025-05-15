using Rin.Engine.Graphics.Textures;

namespace Rin.Engine.Graphics.Descriptors;

public struct SamplerWrite
{
    public SamplerSpec Sampler;
    public uint Index = 0;

    public SamplerWrite(SamplerSpec spec)
    {
        Sampler = spec;
    }
    
}
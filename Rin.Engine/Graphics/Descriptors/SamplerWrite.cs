namespace Rin.Engine.Graphics.Descriptors;

public struct SamplerWrite(SamplerSpec spec)
{
    public SamplerSpec Sampler = spec;
    public uint Index = 0;
}
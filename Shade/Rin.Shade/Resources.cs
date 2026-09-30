using System.Numerics;

namespace Rin.Shade;

public struct SamplerState;

public struct Texture2D
{
    [SlangExpression("@this.Sample(@sampler, @location)")]
    public extern Vector4 Sample(SamplerState sampler, Vector2 location);

    [SlangExpression("@this.SampleLevel(@sampler, @location, @level)")]
    public extern Vector4 SampleLevel(SamplerState sampler, Vector2 location, float level);

    [SlangExpression("@this.Load(int3(@x, @y, @mip))")]
    public extern Vector4 Load(int x, int y, int mip);

    [SlangStatement("@this.GetDimensions(@mip, @width, @height, @levels);")]
    public extern void GetDimensions(uint mip, out uint width, out uint height, out uint levels);
}

public struct Texture2DArray
{
    [SlangExpression("@this.Sample(@sampler, @location)")]
    public extern Vector4 Sample(SamplerState sampler, Vector3 location);
}

public struct TextureCube
{
    [SlangExpression("@this.Sample(@sampler, @direction)")]
    public extern Vector4 Sample(SamplerState sampler, Vector3 direction);
}

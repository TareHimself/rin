using System.Numerics;

namespace Rin.Shade;

/// <summary>
/// Opaque sampler binding.
/// </summary>
public struct SamplerState;

/// <summary>
/// A 2D texture bound to a shader.
/// </summary>
public struct Texture2D
{
    /// <summary>
    /// Samples the texture at a normalized coordinate.
    /// </summary>
    [SlangExpression("@this.Sample(@sampler, @location)")]
    public extern Vector4 Sample(SamplerState sampler, Vector2 location);

    /// <summary>
    /// Samples the texture at an explicit mip level.
    /// </summary>
    [SlangExpression("@this.SampleLevel(@sampler, @location, @level)")]
    public extern Vector4 SampleLevel(SamplerState sampler, Vector2 location, float level);

    /// <summary>
    /// Reads one texel without filtering.
    /// </summary>
    [SlangExpression("@this.Load(int3(@x, @y, @mip))")]
    public extern Vector4 Load(int x, int y, int mip);

    /// <summary>
    /// Gets the size of a mip level and the mip count.
    /// </summary>
    [SlangStatement("@this.GetDimensions(@mip, @width, @height, @levels);")]
    public extern void GetDimensions(uint mip, out uint width, out uint height, out uint levels);
}

/// <summary>
/// A 2D texture array bound to a shader.
/// </summary>
public struct Texture2DArray
{
    /// <summary>
    /// Samples the array at a coordinate whose Z is the layer.
    /// </summary>
    [SlangExpression("@this.Sample(@sampler, @location)")]
    public extern Vector4 Sample(SamplerState sampler, Vector3 location);
}

/// <summary>
/// A cube texture bound to a shader.
/// </summary>
public struct TextureCube
{
    /// <summary>
    /// Samples the cube map along a direction.
    /// </summary>
    [SlangExpression("@this.Sample(@sampler, @direction)")]
    public extern Vector4 Sample(SamplerState sampler, Vector3 direction);
}

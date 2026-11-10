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

/// <summary>
/// A raw buffer read and written by byte offset. Offsets must be multiples of 4. The atomics are unsigned only,
/// since byte-address buffers compare unsigned.
/// </summary>
public struct RWByteAddressBuffer
{
    /// <summary>
    /// Reads a value at a byte offset.
    /// </summary>
    [SlangExpression("@this.Load<@T0>(@offset)")]
    public extern T Load<T>(uint offset) where T : unmanaged;

    /// <summary>
    /// Writes a value at a byte offset.
    /// </summary>
    [SlangStatement("@this.Store<@T0>(@offset, @value);")]
    public extern void Store<T>(uint offset, T value) where T : unmanaged;

    /// <summary>
    /// Gets the buffer's size in bytes.
    /// </summary>
    [SlangStatement("@this.GetDimensions(@size);")]
    public extern void GetDimensions(out uint size);

    /// <summary>
    /// Atomically adds <paramref name="value" /> to the uint at <paramref name="offset" />.
    /// </summary>
    [SlangStatement("@this.InterlockedAdd(@offset, @value, @original);")]
    public extern void InterlockedAdd(uint offset, uint value, out uint original);

    [SlangStatement("@this.InterlockedAdd(@offset, @value);")]
    public extern void InterlockedAdd(uint offset, uint value);

    /// <summary>
    /// Atomically stores the smaller of the uint at <paramref name="offset" /> and <paramref name="value" />.
    /// </summary>
    [SlangStatement("@this.InterlockedMin(@offset, @value, @original);")]
    public extern void InterlockedMin(uint offset, uint value, out uint original);

    [SlangStatement("@this.InterlockedMin(@offset, @value);")]
    public extern void InterlockedMin(uint offset, uint value);

    /// <summary>
    /// Atomically stores the larger of the uint at <paramref name="offset" /> and <paramref name="value" />.
    /// </summary>
    [SlangStatement("@this.InterlockedMax(@offset, @value, @original);")]
    public extern void InterlockedMax(uint offset, uint value, out uint original);

    [SlangStatement("@this.InterlockedMax(@offset, @value);")]
    public extern void InterlockedMax(uint offset, uint value);

    /// <summary>
    /// Atomically ands <paramref name="value" /> into the uint at <paramref name="offset" />.
    /// </summary>
    [SlangStatement("@this.InterlockedAnd(@offset, @value, @original);")]
    public extern void InterlockedAnd(uint offset, uint value, out uint original);

    [SlangStatement("@this.InterlockedAnd(@offset, @value);")]
    public extern void InterlockedAnd(uint offset, uint value);

    /// <summary>
    /// Atomically ors <paramref name="value" /> into the uint at <paramref name="offset" />.
    /// </summary>
    [SlangStatement("@this.InterlockedOr(@offset, @value, @original);")]
    public extern void InterlockedOr(uint offset, uint value, out uint original);

    [SlangStatement("@this.InterlockedOr(@offset, @value);")]
    public extern void InterlockedOr(uint offset, uint value);

    /// <summary>
    /// Atomically xors <paramref name="value" /> into the uint at <paramref name="offset" />.
    /// </summary>
    [SlangStatement("@this.InterlockedXor(@offset, @value, @original);")]
    public extern void InterlockedXor(uint offset, uint value, out uint original);

    [SlangStatement("@this.InterlockedXor(@offset, @value);")]
    public extern void InterlockedXor(uint offset, uint value);

    /// <summary>
    /// Atomically replaces the uint at <paramref name="offset" />, returning the prior value.
    /// </summary>
    [SlangStatement("@this.InterlockedExchange(@offset, @value, @original);")]
    public extern void InterlockedExchange(uint offset, uint value, out uint original);

    /// <summary>
    /// Atomically replaces the uint at <paramref name="offset" /> with <paramref name="value" /> if it equals
    /// <paramref name="compare" />, returning the prior value.
    /// </summary>
    [SlangStatement("@this.InterlockedCompareExchange(@offset, @compare, @value, @original);")]
    public extern void InterlockedCompareExchange(uint offset, uint compare, uint value, out uint original);
}

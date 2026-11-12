using System.Globalization;
using System.Numerics;
using JetBrains.Annotations;

namespace Rin.Core.Graphics;

[NoReorder]
public record struct Extent2D : IFormattable
{
    [PublicAPI] public uint Width = 0;

    [PublicAPI] public uint Height = 0;
    
    public static readonly Extent2D Zero = new Extent2D(0, 0);

    public Extent2D()
    {
    }

    public Extent2D(uint size)
    {
        Width = Height = size;
    }

    public Extent2D(int size)
    {
        Width = Height = (uint)size;
    }

    public Extent2D(uint width, uint height)
    {
        Width = width;
        Height = height;
    }

    public Extent2D(int width, int height)
    {
        Width = (uint)width;
        Height = (uint)height;
    }

    public string ToString(string? format, IFormatProvider? formatProvider)
    {
        var separator = NumberFormatInfo.GetInstance(formatProvider).NumberGroupSeparator;

        return
            $"<{Width.ToString(format, formatProvider)}{separator} {Height.ToString(format, formatProvider)}>";
    }

    /// <summary>
    ///     How many levels a texture of this size has when created with mips, down to 1 x 1.
    /// </summary>
    public readonly uint FullMipLevels => (uint)BitOperations.Log2(uint.Max(uint.Max(Width, Height), 1)) + 1;

    /// <summary>
    ///     The size of mip <paramref name="level" />, where level 0 is this extent.
    /// </summary>
    public readonly Extent2D MipExtent(uint level)
    {
        var shift = (int)uint.Min(level, 31);
        return new Extent2D(uint.Max(Width >> shift, 1), uint.Max(Height >> shift, 1));
    }

    public void Deconstruct(out uint width, out uint height)
    {
        width = Width;
        height = Height;
    }
}
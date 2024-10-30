using rin.Core;
using rin.Core.Math;
using TerraFX.Interop.Vulkan;

namespace rin.Widgets;

public class Size2d(float inWidth, float inHeight) : ICloneable<Size2d>, IEquatable<Size2d>
{
    public float Height = inHeight;
    public float Width = inWidth;

    public Size2d() : this(0, 0)
    {
    }

    public Size2d Clone()
    {
        return new Size2d(Width, Height);
    }

    public bool Equals(Size2d? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Height.Equals(other.Height) && Width.Equals(other.Width);
    }


    public static implicit operator Size2d(VkExtent2D extent)
    {
        return new Size2d(extent.width, extent.height);
    }

    public static implicit operator Size2d(VkExtent3D extent)
    {
        return new Size2d(extent.width, extent.height);
    }

    public static implicit operator Size2d(Vector2<float> p)
    {
        return new Size2d(p.X, p.Y);
    }

    public static implicit operator Vector2<float>(Size2d p)
    {
        return new Vector2<float>(p.Width, p.Height);
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((Size2d)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Height, Width);
    }

    public static implicit operator Size2d(Vector2<int> vec2I)
    {
        return new Size2d()
        {
            Width = vec2I.X,
            Height = vec2I.Y
        };
    }
}
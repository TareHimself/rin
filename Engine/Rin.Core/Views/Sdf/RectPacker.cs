using Rin.Core.Graphics;
using StbRectPackSharp;

namespace Rin.Core.Views.Sdf;

using RectPackPacker = Packer;

public class RectPacker<T>(in Extent2D extent, int padding = 0) : IDisposable
{
    private readonly RectPackPacker _packer = new((int)extent.Width, (int)extent.Height);
    private readonly int _padding2X = padding * 2;
    public Extent2D Extent { get; } = extent;
    public int Padding => padding;

    public IEnumerable<PackedRect<T>> Rects => _packer.PackRectangles.Select(c => new PackedRect<T>
    {
        X = c.X + padding,
        Y = c.Y + padding,
        Width = c.Width - _padding2X,
        Height = c.Height - _padding2X,
        Data = (T)c.Data
    });

    public void Dispose()
    {
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }

    public bool Pack(int width, int height, T data)
    {
        return _packer.PackRect(width + _padding2X, height + _padding2X, data) != null;
    }

    public bool Pack(in Extent2D extent, T data)
    {
        return Pack((int)extent.Width, (int)extent.Height, data);
    }

    private void ReleaseUnmanagedResources()
    {
        _packer.Dispose();
    }

    ~RectPacker()
    {
        ReleaseUnmanagedResources();
    }
}
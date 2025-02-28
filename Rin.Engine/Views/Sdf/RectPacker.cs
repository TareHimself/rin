using StbRectPackSharp;

namespace Rin.Engine.Views.Sdf;

using RectPackPacker = Packer;

public class RectPacker<T>(int packerWidth, int packerHeight, int packerPadding = 0) : IDisposable
{
    private readonly RectPackPacker _packer = new(packerWidth, packerHeight);
    private readonly int _padding2X = packerPadding * 2;
    public int Width => packerWidth;
    public int Height => packerHeight;
    public int Padding => packerPadding;

    public IEnumerable<PackedRect<T>> Rects => _packer.PackRectangles.Select(c => new PackedRect<T>
    {
        X = c.X + packerPadding,
        Y = c.Y + packerPadding,
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

    private void ReleaseUnmanagedResources()
    {
        _packer.Dispose();
    }

    ~RectPacker()
    {
        ReleaseUnmanagedResources();
    }
}
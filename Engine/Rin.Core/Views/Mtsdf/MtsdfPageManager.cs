using Rin.Core.Graphics;
using Rin.Core.Shared;
using StbRectPackSharp;

namespace Rin.Core.Views.Mtsdf;

public class MtsdfPageManager : IDisposable
{
    private int _padding = 2;
    private class MtsdfPage : IDisposable
    {
        public ResourceHandle TextureHandle => _textureHandle;
        public Packer Packer { get; set; }
        private readonly ResourceHandle _textureHandle;
        private readonly IGraphicsModule _graphicsModule;

        public MtsdfPage(Extent2D extent,IGraphicsModule graphicsModule)
        {
            _graphicsModule = graphicsModule;
            using var hostImage = HostImage.Create(extent, ImageFormat.RGBA8);
            // Background must read as "outside" to the MTSDF shader (median >= 0.5), matching the fill used
            // when baking static atlases - clearing to 0 reads as "inside" and produces opaque seams wherever
            // sampling touches unwritten padding.
            using var clear = hostImage.Mutate(c => c.Fill(255,255,255,0));
            _textureHandle = clear.CreateTexture(graphicsModule: graphicsModule);
            Packer = new Packer(initialWidth: (int)extent.Width, initialHeight: (int)extent.Height);
        }

        private void ReleaseUnmanagedResources()
        {
            _graphicsModule.FreeResourceHandles(_textureHandle);
            Packer.Dispose();
        }

        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        ~MtsdfPage()
        {
            ReleaseUnmanagedResources();
        }
    }

    private Extent2D _extent;
    private readonly Lock _pageLock = new Lock();
    private int _pageIndex = -1;
    private readonly List<MtsdfPage> _pages = [];
    private bool _disposed = false;
    private IGraphicsModule _graphicsModule;

    public MtsdfPageManager(in Extent2D extent,IGraphicsModule? graphicsModule = null)
    {
        _extent = extent;
        _graphicsModule = graphicsModule ?? IGraphicsModule.Get();
    }

    /// <summary>
    ///     Packs the glyph into a page and queues its upload, which lands ahead of the first draw that samples it.
    /// </summary>
    public MtsdfInfo AddMtsdf(in Extent2D extent, ReadOnlyMemory<byte> data)
    {
        lock (_pageLock)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            var rect = PackRect(extent);
            var atlas = _pages[_pageIndex].TextureHandle;
            _graphicsModule.QueueTextureUpload(atlas, data, rect.Extent, rect.Offset);
            return new MtsdfInfo { Atlas = atlas, Rect = rect };
        }
    }

    private RectUint PackRect(in Extent2D extent)
    {
        if (_pageIndex == -1) AddPage();

        var width = _padding * 2 + (int)extent.Width;
        var height = _padding * 2 + (int)extent.Height;
        var packed = _pages[_pageIndex].Packer.PackRect(width, height, null);
        if (packed is null)
        {
            AddPage();
            packed = _pages[_pageIndex].Packer.PackRect(width, height, null)
                     ?? throw new ArgumentException("The glyph is larger than an atlas page.", nameof(extent));
        }

        return new RectUint
        {
            Offset =
            {
                X = (uint)(packed.X + _padding),
                Y = (uint)(packed.Y + _padding)
            },
            Extent = extent
        };
    }

    private void AddPage()
    {
        _pages.Add(new MtsdfPage(_extent, _graphicsModule));
        _pageIndex = _pages.Count - 1;
    }

    private void ReleaseUnmanagedResources()
    {
        lock (_pageLock)
        {
            _disposed = true;
            foreach (var page in _pages)
            {
                page.Dispose();
            }

            _pages.Clear();
            _pageIndex = 0;
        }
    }

    public void Dispose()
    {
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }

    ~MtsdfPageManager()
    {
        ReleaseUnmanagedResources();
    }
}
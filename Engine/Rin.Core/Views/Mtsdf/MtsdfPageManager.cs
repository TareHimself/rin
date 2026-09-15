using Rin.Core.Extensions;
using Rin.Core.Graphics;
using Rin.Core.Shared;
using StbRectPackSharp;

namespace Rin.Core.Views.Mtsdf;

public enum MtsdfState
{
    None,
    Assigned,
    Ready
}

public struct MtsdfInfo
{
    public int Id;
    public MtsdfState State;
    public ResourceHandle Atlas;
    public RectUint Rect;
}

public class MtsdfPageManager : IDisposable
{
    private int _padding = 2;
    private class MtsdfPage : IDisposable
    {
        public Task<ResourceHandle> TextureHandleTask { get; set; }
        public Packer Packer { get; set; }

        public MtsdfPage(Extent2D extent,IGraphicsModule graphicsModule)
        {
            using var hostImage = HostImage.Create(extent, ImageFormat.RGBA8);
            // Background must read as "outside" to the MTSDF shader (median >= 0.5), matching the fill used
            // when baking static atlases - clearing to 0 reads as "inside" and produces opaque seams wherever
            // sampling touches unwritten padding.
            using var clear = hostImage.Mutate(c => c.Fill(255,255,255,0));
            TextureHandleTask = clear.CreateTexture(out _,graphicsModule: graphicsModule);
            Packer = new Packer(initialWidth: (int)extent.Width, initialHeight: (int)extent.Height);
        }

        private void ReleaseUnmanagedResources()
        {
            TextureHandleTask.Then(static c => IGraphicsModule.Get().FreeResourceHandles(c));
            Packer.Dispose();
        }

        private void Dispose(bool disposing)
        {
            ReleaseUnmanagedResources();
            if (disposing)
            {
                TextureHandleTask.Dispose();
                Packer.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~MtsdfPage()
        {
            Dispose(false);
        }
    }

    private Extent2D _extent;
    private readonly Lock _pageLock = new Lock();
    private int _pageIndex = -1;
    private readonly List<MtsdfPage> _pages = [];
    private readonly List<Task<MtsdfInfo>> _infos = [];
    private bool _disposed = false;
    private IGraphicsModule _graphicsModule;

    public MtsdfPageManager(in Extent2D extent,IGraphicsModule? graphicsModule = null)
    {
        _extent = extent;
        _graphicsModule = graphicsModule ?? IGraphicsModule.Get();
    }

    /// <summary>
    ///     Carries everything the <see cref="AddMtsdf" /> continuations need so they can be passed as
    ///     <c>static</c> lambdas + explicit state (see <see cref="TaskExtensions.Then{T,TState,TV}" />) instead of
    ///     each capturing <c>this</c>/<c>data</c>/<c>info</c>/<c>completionSource</c> into its own closure - this
    ///     runs once per glyph rasterized, so avoiding a closure (and its delegate) per continuation matters.
    /// </summary>
    private sealed class PendingUpload(
        MtsdfPageManager owner,
        MtsdfInfo info,
        ReadOnlyMemory<byte> data,
        TaskCompletionSource<MtsdfInfo> completionSource)
    {
        public readonly MtsdfPageManager Owner = owner;
        public MtsdfInfo Info = info;
        public readonly ReadOnlyMemory<byte> Data = data;
        public readonly TaskCompletionSource<MtsdfInfo> CompletionSource = completionSource;
    }

    public Task<MtsdfInfo> AddMtsdf(in Extent2D extent, ReadOnlyMemory<byte> data)
    {
        MtsdfPage? page = null;
        MtsdfInfo info;
        var completionSource = new TaskCompletionSource<MtsdfInfo>();
        lock (_pageLock)
        {
            if (_disposed) return Task.FromException<MtsdfInfo>(new ObjectDisposedException(nameof(MtsdfPageManager)));
            PackerRectangle? packed = null;
            if (_pageIndex == -1)
            {
                _pages.Add(new MtsdfPage(_extent,_graphicsModule));
                _pageIndex = 0;
            }

            while (packed is null)
            {
                packed = _pages[_pageIndex].Packer.PackRect(_padding * 2 + (int)extent.Width, _padding * 2 + (int)extent.Height, null);
                if (packed is not null) continue;
                _pages.Add(new MtsdfPage(_extent,_graphicsModule));
                _pageIndex++;
            }

            page = _pages[_pageIndex];

            var id = _infos.Count;
            info = new MtsdfInfo
            {
                Id = id,
                State = MtsdfState.Assigned,
                Rect = new RectUint
                {
                    Offset =
                    {
                        X = (uint)(packed.X + _padding),
                        Y = (uint)(packed.Y + _padding),
                    },
                    Extent = extent
                },
                Atlas = ResourceHandle.InvalidTexture
            };
            _infos.Add(completionSource.Task);
        }

        var upload = new PendingUpload(this, info, data, completionSource);
        page.TextureHandleTask.Then(static (handle, upload) =>
        {
            if (upload.Owner._disposed) return;
            upload.Owner._graphicsModule.UploadToTexture(handle, upload.Data, upload.Info.Rect.Extent, upload.Info.Rect.Offset)
                .Then(static state =>
                {
                    var (upload, handle) = state;
                    lock (upload.Owner._pageLock)
                    {
                        if (upload.Owner._disposed) return;
                        upload.Info.Atlas = handle;
                        upload.Info.State = MtsdfState.Ready;
                        upload.CompletionSource.SetResult(upload.Info);
                    }
                }, (upload, handle));
        }, upload);

        return completionSource.Task;
    }

    public Task<MtsdfInfo> GetInfo(int id)
    {
        lock (_pageLock)
        {
            return _infos[id];
        }
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
            _infos.Clear();
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
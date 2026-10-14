using Examples.AudioPlayer.Library;
using Rin.Core.Extensions;
using Rin.Core.Graphics;
using Rin.Core.Views.Content;

namespace Examples.AudioPlayer.Playback;

/// <summary>
///     A track in the queue and its cover texture, which loads in the background after the entry is made.
/// </summary>
public sealed class QueueEntry : IDisposable
{
    private readonly CancellationTokenSource _cancellation = new();
    private bool _disposed;

    public QueueEntry(TrackInfo info)
    {
        Info = info;
        LoadCover(_cancellation.Token).DispatchMain(OnCoverLoaded);
    }

    public TrackInfo Info { get; }

    public ImageInfo Cover { get; private set; } = new(ResourceHandle.InvalidTexture, Extent2D.Zero);

    public bool HasCover => Cover.Handle != ResourceHandle.InvalidTexture;

    public event Action? CoverChanged;

    private async Task<ImageInfo> LoadCover(CancellationToken cancellation)
    {
        try
        {
            using var image = await CoverFinder.Find(Info, cancellation);
            if (image is null) return Cover;

            var extent = image.Extent;
            await image.CreateTexture(out var handle);
            return new ImageInfo(handle, extent);
        }
        catch (OperationCanceledException)
        {
            return Cover;
        }
    }

    private void OnCoverLoaded(ImageInfo cover)
    {
        if (cover.Handle == ResourceHandle.InvalidTexture) return;

        if (_disposed)
        {
            IGraphicsModule.Get().FreeResourceHandles(cover.Handle);
            return;
        }

        Cover = cover;
        CoverChanged?.Invoke();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _cancellation.Cancel();
        _cancellation.Dispose();

        if (!HasCover) return;
        IGraphicsModule.Get().FreeResourceHandles(Cover.Handle);
        Cover = new ImageInfo(ResourceHandle.InvalidTexture, Extent2D.Zero);
    }
}

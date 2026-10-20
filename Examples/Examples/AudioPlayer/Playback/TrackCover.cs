using Examples.AudioPlayer.Library;
using Rin.Core.Extensions;
using Rin.Core.Graphics;
using Rin.Core.Views.Content;

namespace Examples.AudioPlayer.Playback;

/// <summary>
///     A track's cover as a texture and the palette taken from it, found on a worker after construction and freed
///     on dispose.
/// </summary>
public sealed class TrackCover : IDisposable
{
    private readonly CancellationTokenSource _cancellation = new();
    private bool _disposed;

    public TrackCover(TrackInfo track)
    {
        Track = track;
        Load(_cancellation.Token).DispatchMain(OnLoaded);
    }

    public TrackInfo Track { get; }

    public ImageInfo Image { get; private set; } = new(ResourceHandle.InvalidTexture, Extent2D.Zero);

    public CoverPalette Palette { get; private set; } = CoverPalette.Default;

    public bool HasImage => Image.Handle != ResourceHandle.InvalidTexture;

    public event Action? Changed;

    private async Task<(ImageInfo Image, CoverPalette Palette)> Load(CancellationToken cancellation)
    {
        try
        {
            using var image = await CoverFinder.Find(Track, cancellation);
            if (image is null) return (Image, Palette);

            var palette = CoverPalette.From(image);
            var extent = image.Extent;
            await image.CreateTexture(out var handle);
            return (new ImageInfo(handle, extent), palette);
        }
        catch (OperationCanceledException)
        {
            return (Image, Palette);
        }
    }

    private void OnLoaded((ImageInfo Image, CoverPalette Palette) loaded)
    {
        if (loaded.Image.Handle == ResourceHandle.InvalidTexture) return;

        if (_disposed)
        {
            IGraphicsModule.Get().FreeResourceHandles(loaded.Image.Handle);
            return;
        }

        Image = loaded.Image;
        Palette = loaded.Palette;
        Changed?.Invoke();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _cancellation.Cancel();
        _cancellation.Dispose();

        if (!HasImage) return;
        IGraphicsModule.Get().FreeResourceHandles(Image.Handle);
        Image = new ImageInfo(ResourceHandle.InvalidTexture, Extent2D.Zero);
    }
}

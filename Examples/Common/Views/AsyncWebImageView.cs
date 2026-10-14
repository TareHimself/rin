using System.Numerics;
using Rin.Core;
using Rin.Core.Extensions;
using Rin.Core.Graphics;
using Rin.Core.Views;
using Rin.Core.Views.Content;
using Rin.Core.Views.Graphics;
using Rin.Core.Views.Graphics.Quads;

namespace Examples.Common.Views;

public class AsyncWebImageView : CoverImageView
{
    private static readonly HttpClient Client = new();
    private bool _disposed;

    public AsyncWebImageView(string uri)
    {
        LoadFile(uri).ConfigureAwait(false);
    }

    public event Action<bool>? OnLoaded;

    private async Task LoadFile(string uri)
    {
        try
        {
            var stream = await Client.GetStreamAsync(uri);
            using var img = await Task.Run(() => HostImage.Create(stream));

            await img.CreateTexture(out var texId);
            var extent = img.Extent;
            IApplication.Get().MainDispatcher.Enqueue(() =>
            {
                if (_disposed)
                {
                    IGraphicsModule.Get().FreeResourceHandles(texId);
                    return;
                }

                Image = new ImageInfo(texId, extent);
                OnLoaded?.Invoke(true);
            });
        }
        catch (Exception)
        {
            IApplication.Get().MainDispatcher.Enqueue(() =>
            {
                if (!_disposed) OnLoaded?.Invoke(false);
            });
        }
    }

    public override void Dispose()
    {
        base.Dispose();
        if (_disposed) return;
        _disposed = true;

        var handle = Image.Handle;
        if (handle == ResourceHandle.InvalidTexture) return;
        Image = new ImageInfo(ResourceHandle.InvalidTexture, Extent2D.Zero);
        IGraphicsModule.Get().FreeResourceHandles(handle);
    }

    public override void CollectContent(in Matrix4x4 transform, CommandList commands)
    {
        base.CollectContent(transform, commands);
        commands.AddRect(transform, GetContentSize(), Color.Black with { A = 0.5f }, BorderRadius);
    }
}
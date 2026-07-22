using System.Numerics;
using Rin.Core;
using Rin.Core.Extensions;
using Rin.Core.Graphics;
using Rin.Core.Views;
using Rin.Core.Views.Content;
using Rin.Core.Views.Graphics;
using Rin.Core.Views.Graphics.Quads;

namespace rin.Examples.Common.Views;

public class AsyncWebImageView : CoverImageView
{
    private static readonly HttpClient Client = new();

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

            await img.CreateTexture(out var texId).Dispatch(IApplication.Get().MainDispatcher, () =>
            {
                ImageHandle = texId;
                OnLoaded?.Invoke(true);
            });
        }
        catch (Exception e)
        {
            OnLoaded?.Invoke(false);
        }
    }

    public override void CollectContent(in Matrix4x4 transform, CommandList commands)
    {
        base.CollectContent(transform, commands);
        commands.AddRect(transform, GetContentSize(), Color.Black with { A = 0.5f }, BorderRadius);
    }
}
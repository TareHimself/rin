using System.Numerics;
using Rin.Framework;
using Rin.Framework.Extensions;
using Rin.Framework.Graphics;
using Rin.Framework.Views;
using Rin.Framework.Views.Content;
using Rin.Framework.Views.Graphics;
using Rin.Framework.Views.Graphics.Quads;

namespace rin.Examples.Common.Views;

public class AsyncWebImageView : CoverImageView
{
    public AsyncWebImageView(string uri)
    {
        LoadFile(uri).ConfigureAwait(false);
    }

    public event Action<bool>? OnLoaded;

    private async Task LoadFile(string uri)
    {
        try
        {
            using var client = new HttpClient();
            var stream = await client.GetStreamAsync(uri);
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
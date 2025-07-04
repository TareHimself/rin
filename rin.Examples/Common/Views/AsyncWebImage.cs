using System.Numerics;
using Rin.Engine;
using Rin.Engine.Extensions;
using Rin.Engine.Graphics;
using Rin.Engine.Views;
using Rin.Engine.Views.Content;
using Rin.Engine.Views.Graphics;
using Rin.Engine.Views.Graphics.Quads;

namespace rin.Examples.Common.Views;

public class AsyncWebImage : CoverImage
{
    public AsyncWebImage(string uri)
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
            var (texId, task) = img.CreateTexture();
            task.Dispatch(SEngine.Get().GetMainDispatcher(), () =>
            {
                ImageId = texId;
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
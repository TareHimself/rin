using System.Numerics;
using Rin.Engine;
using Rin.Engine.Extensions;
using Rin.Engine.Graphics;
using Rin.Engine.Views;
using Rin.Engine.Views.Content;
using Rin.Engine.Views.Graphics;
using Rin.Engine.Views.Graphics.Quads;
using SixLabors.ImageSharp.PixelFormats;
using Image = SixLabors.ImageSharp.Image;

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
            using var img = await Image.LoadAsync<Rgba32>(stream);
            var (texId, task) = SGraphicsModule.Get().CreateTexture(img.ToBuffer(),
                new Extent3D
                {
                    Width = (uint)img.Width,
                    Height = (uint)img.Height
                },
                ImageFormat.RGBA8);
            task.DispatchAfter(SEngine.Get().GetMainDispatcher(), () =>
            {
                TextureId = texId;
                OnLoaded?.Invoke(true);
            });
        }
        catch (Exception e)
        {
            OnLoaded?.Invoke(false);
        }
    }

    public override void CollectContent(Matrix4x4 transform, CommandList commands)
    {
        base.CollectContent(transform, commands);
        commands.AddRect(transform, GetContentSize(), Color.Black with { A = 0.5f }, BorderRadius);
    }
}
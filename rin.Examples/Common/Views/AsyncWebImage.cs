using rin.Framework.Core.Extensions;
using rin.Framework.Core.Math;
using rin.Framework.Graphics;
using rin.Framework.Views;
using rin.Framework.Views.Animation;
using rin.Framework.Views.Content;
using rin.Framework.Views.Graphics;
using rin.Framework.Views.Graphics.Quads;
using SixLabors.ImageSharp.PixelFormats;
using Image = SixLabors.ImageSharp.Image;

namespace rin.Examples.Common.Views;

public class AsyncWebImage : CoverImage
{
    public event Action<bool> OnLoaded;
    
    public AsyncWebImage(string uri) : base()
    {
        LoadFile(uri).ConfigureAwait(false);
    }

    protected async Task LoadFile(string uri)
    {
        try
        {
            using var client = new HttpClient();
            var stream = await client.GetStreamAsync(uri);
            using var img = await Image.LoadAsync<Rgba32>(stream);
            using var imgData = img.ToBuffer();
            await SGraphicsModule.Get().GetTextureManager().CreateTexture(imgData,
                new Extent3D
                {
                    Width = (uint)img.Width,
                    Height = (uint)img.Height
                },
                ImageFormat.RGBA8).Then(c => TextureId = c);
            OnLoaded?.Invoke(true);
        }
        catch (Exception e)
        {
            OnLoaded?.Invoke(false);
        }
    }

    public override void CollectContent(Mat3 transform, PassCommands commands)
    {
        base.CollectContent(transform, commands);
        commands.AddRect(transform, GetContentSize(), Color.Black.Clone(a: 0.5f),BorderRadius);
    }
}
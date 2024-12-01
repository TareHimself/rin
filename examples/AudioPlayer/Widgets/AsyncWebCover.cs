
using rin.Framework.Core.Animation;
using rin.Framework.Core.Extensions;
using rin.Framework.Core.Math;
using rin.Framework.Graphics;
using rin.Framework.Widgets;
using rin.Framework.Widgets.Animation;
using rin.Framework.Widgets.Content;
using rin.Framework.Widgets.Graphics;
using rin.Framework.Widgets.Graphics.Quads;
using SixLabors.ImageSharp;
using ImageSharpImage = SixLabors.ImageSharp.Image;
using SixLabors.ImageSharp.PixelFormats;
using TerraFX.Interop.Vulkan;
using Color = rin.Framework.Widgets.Color;

namespace AudioPlayer.Widgets;

public class AsyncWebCover : CoverImage
{
    public AsyncWebCover(string uri) : base()
    {
        LoadFile(uri).ConfigureAwait(false);
    }

    private async Task LoadFile(string uri)
    {
        using var client = new HttpClient();
        var stream = await client.GetStreamAsync(uri);
        using var img = await ImageSharpImage.LoadAsync<Rgba32>(stream);
        using var imgData = img.ToBuffer();
        await SGraphicsModule.Get().GetResourceManager().CreateTexture(imgData,
            new VkExtent3D
            {
                width = (uint)img.Width,
                height = (uint)img.Height,
                depth = 1
            },
            ImageFormat.Rgba8).Then(c => TextureId = c);
        await img.SaveAsPngAsync("./latest.png");
        this.Parent?.Parent
            ?.ScaleTo(new Vector2<float>(1.0f, 1.0f), 1.0f, 0.2f, easingFunction: EasingFunctions.EaseInExpo).After();
    }

    public override void CollectContent(Matrix3 transform, DrawCommands drawCommands)
    {
        base.CollectContent(transform, drawCommands);
        drawCommands.AddRect(transform, GetContentSize(), Color.Black.Clone(a: 0.5f),BorderRadius);
    }
}
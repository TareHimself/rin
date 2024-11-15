using rin.Core.Extensions;
using rin.Graphics;
using rin.Widgets;
using rin.Widgets.Content;
using rin.Widgets.Graphics;
using rin.Widgets.Graphics.Quads;
using SixLabors.ImageSharp;
using ImageSharpImage = SixLabors.ImageSharp.Image;
using SixLabors.ImageSharp.PixelFormats;
using TerraFX.Interop.Vulkan;
using Color = rin.Widgets.Color;

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
    }

    public override void CollectContent(TransformInfo info, DrawCommands drawCommands)
    {
        base.CollectContent(info, drawCommands);
        drawCommands.AddRect(info.Transform, GetContentSize(), Color.Black.Clone(a: 0.5f),BorderRadius);
    }
}
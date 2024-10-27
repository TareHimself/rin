using aerox.Runtime.Graphics;
using aerox.Runtime.Widgets;
using aerox.Runtime.Widgets.Content;
using aerox.Runtime.Widgets.Graphics;
using aerox.Runtime.Widgets.Graphics.Quads;
using SixLabors.ImageSharp;
using ImageSharpImage = SixLabors.ImageSharp.Image;
using SixLabors.ImageSharp.PixelFormats;
using TerraFX.Interop.Vulkan;
using Color = aerox.Runtime.Widgets.Color;

namespace AudioPlayer.Widgets;

public class AsyncWebCover : WCoverImage
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
        TextureId = SGraphicsModule.Get().GetResourceManager().CreateTexture(imgData, new VkExtent3D
            {
                width = (uint)img.Width,
                height = (uint)img.Height,
                depth = 1
            },
            ImageFormat.Rgba8);
        await img.SaveAsPngAsync("./latest.png");
    }

    public override void CollectContent(TransformInfo info, DrawCommands drawCommands)
    {
        base.CollectContent(info, drawCommands);
        drawCommands.AddQuads(new Quad(GetContentSize(), info.Transform)
        {
            Color = Color.Black.Clone(a: 0.5f),
            BorderRadius = BorderRadius
        });
    }
}
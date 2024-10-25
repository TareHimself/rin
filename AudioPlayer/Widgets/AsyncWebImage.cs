using aerox.Runtime.Graphics;
using aerox.Runtime.Widgets;
using SixLabors.ImageSharp;
using ImageSharpImage = SixLabors.ImageSharp.Image;
using SixLabors.ImageSharp.PixelFormats;
using TerraFX.Interop.Vulkan;
using Image = aerox.Runtime.Widgets.Content.Image;

namespace AudioPlayer.Widgets;

public class AsyncWebImage : Image
{
    public AsyncWebImage(string uri) : base()
    {
        LoadFile(uri).ConfigureAwait(false);
    }

    private async Task LoadFile(string uri)
    {
        using var client = new HttpClient();
        Stream stream = await client.GetStreamAsync(uri);
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

}
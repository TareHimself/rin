using System.Runtime.InteropServices;
using aerox.Runtime.Graphics;
using SixLabors.Fonts;
using TerraFX.Interop.Vulkan;

namespace aerox.Runtime.Widgets;

public class MsdfGenerator(FontFamily family)
{
    private readonly FontFamily _family = family;

    private void GenerateMsdf(char c, MsdfFont msdfFont, Font font)
    {
        var msdfRenderer = new MsdfRenderer();
        TextRenderer.RenderTextTo(msdfRenderer, $"{c}", new TextOptions(font));
        msdfRenderer.Generate((data, width, height, dataSize) =>
        {
            var copiedData = new byte[dataSize];
            Marshal.Copy(data, copiedData, 0, (int)dataSize);
            msdfFont.Add(c, new Texture(copiedData, new VkExtent3D
                {
                    width = width,
                    height = height,
                    depth = 1
                }, ImageFormat.Rgba8, ImageFilter.Linear, ImageTiling.ClampEdge, false, $"msdf => {c}"));
        });
        msdfRenderer.Dispose();
    }

    public async Task<MsdfFont> GenerateFont(float size)
    {
        var msdfFont = new MsdfFont(_family);
        var font = _family.CreateFont(size);
        var chars = Enumerable.Range(32, 127).Select(i => (char)i).ToArray();

        await Task.WhenAll(chars.Select(c => Task.Run(() => GenerateMsdf(c, msdfFont, font))));

        return msdfFont;
    }
}
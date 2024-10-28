using aerox.Runtime.Graphics;
using aerox.Runtime.Math;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using StbRectPackSharp;
using TerraFX.Interop.Vulkan;

namespace aerox.Runtime.Widgets.Mtsdf;

public class MtsdfGenerator(FontFamily family)
{
    private struct GeneratedMtsdf(Image mtsdf, int id)
    {
        public readonly Image Mtsdf = mtsdf;
        public readonly int Id = id;
    }
    
    private readonly FontFamily _family = family;
    private const int GeneratePadding = 3;
    private const int AtlasPadding = 1;
    
    private GeneratedMtsdf? GenerateMtsdf(Glyph glyph, Font font)
    {
        using var mtsdfRenderer = new MtsdfRenderer();
        var codepoint = glyph.GlyphMetrics.CodePoint;
        var options = new TextOptions(font);
        var targetStr = codepoint.ToString();
        TextRenderer.RenderTextTo(mtsdfRenderer,targetStr,options);
        
        var result = mtsdfRenderer.Generate(GeneratePadding,3f,30f);
        
        if (result == null) return null;

        Span<byte> data = result.Data;
        
        var img = Image.LoadPixelData<Rgba32>(data, result.Width, result.Height);
        
        img.Mutate(i => i.Pad(result.Width + (AtlasPadding * 2), result.Height + (AtlasPadding * 2),new SixLabors.ImageSharp.Color(new Rgba32(255,255,255,255))));
        
        result.Data.Dispose();
        
        return new GeneratedMtsdf(img, codepoint.Value);
    }

    public async Task<MtsdfFont> GenerateFont(float size)
    {
        const int rectSize = 512;
        
        var font = _family.CreateFont(size);

        var allGlyphs = font.FontMetrics.GetAvailableCodePoints().Where(c => c is { IsAscii: true, Value: > 32 } && c.Value != 127).Select(c =>
        {
            font.TryGetGlyphs(c, out var glyphs);
            return glyphs?[0];
        }).Where(c => c != null).Select(c => c!.Value);
        
        var mtsdfs = (await Task.WhenAll(allGlyphs.Select(c => Task.Run(() => GenerateMtsdf(c,font))))).Where(c => c != null).Select(c => (GeneratedMtsdf)c!).ToArray();
        
        List<Packer> packers = [new Packer(rectSize, rectSize)];
        
        for(var i = 0; i < mtsdfs.Length; i++)
        {
            var mtsdf = mtsdfs[i];
            
            var targetPacker = packers.Last();
            
            if (targetPacker.PackRect(mtsdf.Mtsdf.Width, mtsdf.Mtsdf.Height, mtsdf) != null) continue;
            
            packers.Add(new Packer(rectSize,rectSize));
            
            i--;
        }

        var atlases = packers.Select(_ => new Image<Rgba32>(rectSize,rectSize, new Rgba32(0, 0, 0, 0))).ToArray();

        List<MtsdfAtlasGlyph> atlasGlyphs = [];
        
        for (var i = 0; i < packers.Count; i++)
        {
            var packer = packers[i];
            
            var atlas = atlases[i];
            var atlasIdx = i;
            var atlasSize = new Vector2<float>(atlas.Width, atlas.Height);
            atlas.Mutate(o =>
            {
                foreach (var rect in packer.PackRectangles)
                {
                    var generated = (GeneratedMtsdf)rect.Data;
                    var offset = new Vector2<float>(rect.X + AtlasPadding + GeneratePadding, rect.Y + AtlasPadding + GeneratePadding);
                    var offset2 = offset + new Vector2<float>(rect.Width - (AtlasPadding * 2 + GeneratePadding * 2),
                        rect.Height - (AtlasPadding * 2 + GeneratePadding * 2));
                    offset /= atlasSize;
                    offset2 /= atlasSize;
                    
                    atlasGlyphs.Add(new MtsdfAtlasGlyph
                    {
                        Id = generated.Id,
                        AtlasIdx = atlasIdx,
                        Coordinates = new Vector4<float>(offset,offset2)
                    });

                    o.DrawImage(generated.Mtsdf, new Point(rect.X, rect.Y), 1F);
                }
            });
            
            await atlas.SaveAsPngAsync($"./atlas_{i}.png");
        }

        var resourceManager = SGraphicsModule.Get().GetResourceManager();
        return new MtsdfFont(_family,atlases.Select((c, idx) =>
        {
            using var buffer = c.ToBuffer();
            return resourceManager.CreateTexture(buffer, new VkExtent3D()
                {
                    width = (uint)c.Width,
                    height = (uint)c.Height,
                    depth = 1
                }, ImageFormat.Rgba8, ImageFilter.Linear, ImageTiling.ClampEdge, false, $"{_family.Name} Atlas {idx}");
        }).ToArray(),atlasGlyphs);
    }
}
using rin.Core.Extensions;
using rin.Graphics;
using rin.Core.Math;
using rin.Sdf;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using TerraFX.Interop.Vulkan;

namespace rin.Widgets.Sdf;

public class SdfFontGenerator(FontFamily family)
{
    private struct GeneratedMtsdf(Image mtsdf, int id)
    {
        public readonly Image Mtsdf = mtsdf;
        public readonly int Id = id;
    }
    
    private readonly FontFamily _family = family;
    private const int GeneratePadding = 2;
    
    private GeneratedMtsdf? GenerateMtsdf(Glyph glyph, Font font)
    {
        using var mtsdfRenderer = new SdfTextRenderer();
        var codepoint = glyph.GlyphMetrics.CodePoint;
        var options = new TextOptions(font);
        var targetStr = codepoint.ToString();
        TextRenderer.RenderTextTo(mtsdfRenderer,targetStr,options);
        
        var result = mtsdfRenderer.Generate(3f,30f);
        
        if (result == null) return null;

        Span<byte> data = result.Data;
        
        var img = Image.LoadPixelData<Rgba32>(data, result.Width, result.Height);
        
        result.Data.Dispose();
        
        return new GeneratedMtsdf(img, codepoint.Value);
    }

    public async Task<SdfFont> GenerateFont(float size,int atlasSize = 512)
    {
        var font = _family.CreateFont(size);

        var allGlyphs = font.FontMetrics.GetAvailableCodePoints().Where(c => c is { IsAscii: true, Value: > 32 } && c.Value != 127).Select(c =>
        {
            font.TryGetGlyphs(c, out var glyphs);
            return glyphs?[0];
        }).Where(c => c != null).Select(c => c!.Value);

        var sdfs = await Task.WhenAll(allGlyphs.Select(c => Task.Run(() => GenerateMtsdf(c, font)))).Then(generated =>
            generated.Where(c => c != null).Select(c => (GeneratedMtsdf)c!).ToArray());
        
        List<RectPacker<GeneratedMtsdf>> packers = [new RectPacker<GeneratedMtsdf>(atlasSize,atlasSize,GeneratePadding)];
        
        for(var i = 0; i < sdfs.Length; i++)
        {
            var mtsdf = sdfs[i];
            
            var targetPacker = packers.Last();
            
            if (targetPacker.Pack(mtsdf.Mtsdf.Width, mtsdf.Mtsdf.Height, mtsdf)) continue;
            
            packers.Add(new RectPacker<GeneratedMtsdf>(atlasSize, atlasSize,GeneratePadding));
            
            i--;
        }

        var atlases = packers.Select(p => new Image<Rgba32>(p.Width,p.Height, new Rgba32(255, 255, 255, 0))).ToArray();

        Dictionary<string,SdfVector> atlasGlyphs = [];
        
        for (var i = 0; i < packers.Count; i++)
        {
            var packer = packers[i];
            
            var atlas = atlases[i];
            var atlasIdx = i;
            atlas.Mutate(o =>
            {
                foreach (var rect in packer.Rects)
                {
                    var generated = rect.Data;
                    var pt1 = new Vector2<float>(rect.X, rect.Y);
                    var pt2 = pt1 + new Vector2<float>(rect.Width,
                        rect.Height);
                    var pt1Coord = pt1 / atlasSize;
                    var pt2Coord = pt2 / atlasSize;
                    var generatedId = generated.Id.ToString();
                    atlasGlyphs.Add(generatedId,new SdfVector
                    {
                        Id = generatedId,
                        AtlasIdx = atlasIdx,
                        Rect = new Vector4<float>(pt1,pt2),
                        Coordinates = new Vector4<float>(pt1Coord,pt2Coord)
                    });

                    o.DrawImage(generated.Mtsdf, new Point(rect.X, rect.Y), 1F);
                }
            });
            
            //await atlas.SaveAsPngAsync($"./atlas_{i}.png");
        }

        var resourceManager = SGraphicsModule.Get().GetResourceManager();
        var atlasIds = atlases.Select((c, idx) =>
        {
            using var buffer = c.ToBuffer();
            return resourceManager.CreateTexture(buffer, new VkExtent3D()
                {
                    width = (uint)c.Width,
                    height = (uint)c.Height,
                    depth = 1
                }, ImageFormat.Rgba8, ImageFilter.Linear, ImageTiling.ClampEdge, false, $"{_family.Name} Atlas {idx}");
        }).WaitAll().ToArray();
        return new SdfFont(_family,atlasIds,atlasGlyphs);
    }
}
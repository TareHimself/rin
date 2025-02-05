using System.Numerics;
using rin.Framework.Core.Math;
using rin.Framework.Graphics;
using rin.Framework.Core.Extensions;
using rin.Sdf;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using TerraFX.Interop.Vulkan;
using SixLaborsTextRenderer = SixLabors.Fonts.TextRenderer;
namespace rin.Framework.Views.Sdf;

public class FontGenerator(FontFamily family)
{
    private struct GeneratedMtsdf(Image mtsdf, int id, Vec2<double> size)
    {
        public readonly Image Mtsdf = mtsdf;
        public readonly int Id = id;
        public Vec2<double> Size = size;
    }
    
    private readonly FontFamily _family = family;
    private const int GeneratePadding = 2;
    
    private GeneratedMtsdf? GenerateMtsdf(Glyph glyph, SixLabors.Fonts.Font font,float pixelRange)
    {
        using var mtsdfRenderer = new TextRenderer();
        var codepoint = glyph.GlyphMetrics.CodePoint;
        var options = new TextOptions(font);
        var targetStr = codepoint.ToString();
        SixLaborsTextRenderer.RenderTextTo(mtsdfRenderer,targetStr,options);
        
        var result = mtsdfRenderer.Generate(3f,pixelRange);
        
        if (result == null) return null;

        Span<byte> data = result.Data;
        
        var img = Image.LoadPixelData<Rgba32>(data, result.PixelWidth, result.PixelHeight);
        
        result.Data.Dispose();
        
        return new GeneratedMtsdf(img, codepoint.Value,new Vec2<double>(result.Width,result.Height));
    }

    public async Task<Font> GenerateFont(float size,int atlasSize = 512,float pixelRange = 12.0f)
    {
        var font = _family.CreateFont(size);

        var allGlyphs = font.FontMetrics.GetAvailableCodePoints().Where(c => c is { IsAscii: true, Value: > 32 } && c.Value != 127).Select(c =>
        {
            font.TryGetGlyphs(c, out var glyphs);
            return glyphs?[0];
        }).Where(c => c != null).Select(c => c!.Value);

        var sdfs = await Task.WhenAll(allGlyphs.Select(c => Task.Run(() => GenerateMtsdf(c, font,pixelRange)))).Then(generated =>
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
                    var pt1 = new Vector2(rect.X, rect.Y);
                    var pt2 = pt1 + generated.Size.ToNumericsVector();
                    var dims = pt2 - pt1;
                    var pt1Coord = pt1 / atlasSize;
                    var pt2Coord = pt2 / atlasSize;
                    var generatedId = generated.Id.ToString();
                    atlasGlyphs.Add(generatedId,new SdfVector
                    {
                        Id = generatedId,
                        AtlasIdx = atlasIdx,
                        X = pt1.X,
                        Y = pt1.Y,
                        Width = dims.X,
                        Height = dims.Y,
                        Coordinates = new Vector4(pt1Coord,pt2Coord.X,pt2Coord.Y),
                        Range = pixelRange
                    });

                    o.DrawImage(generated.Mtsdf, new Point(rect.X, rect.Y), 1F);
                }
            });
            
            await atlas.SaveAsPngAsync($"./atlas_{i}.png");
        }

        var resourceManager = SGraphicsModule.Get().GetTextureManager();
        var atlasIds = atlases.Select((c, idx) =>
        {
            using var buffer = c.ToBuffer();
            return resourceManager.CreateTexture(buffer, new Extent3D()
                {
                    Width = (uint)c.Width,
                    Height = (uint)c.Height,
                }, ImageFormat.RGBA8, ImageFilter.Linear, ImageTiling.ClampEdge, false, $"{_family.Name} Atlas {idx}");
        }).WaitAll().ToArray();
        return new Font(_family,atlasIds,atlasGlyphs);
    }
}
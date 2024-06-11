using System.Runtime.InteropServices;
using aerox.Runtime.Graphics;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using StbRectPackSharp;
using TerraFX.Interop.Vulkan;

namespace aerox.Runtime.Widgets;

public class MtsdfGenerator(FontFamily family)
{
    private struct GeneratedMsdf(Image msdf, char character)
    {
        public Image Msdf = msdf;
        public char Character = character;
    }
    
    private readonly FontFamily _family = family;
    private readonly Mutex _mutex = new Mutex();

    private const int GeneratePadding = 3;
    private const int AtlasPadding = 1;
    
    private GeneratedMsdf? GenerateMsdf(char c, Font font)
    {
        using var msdfRenderer = new MtsdfRenderer();
            
        TextRenderer.RenderTextTo(msdfRenderer, $"{c}", new TextOptions(font));
            
        var result = msdfRenderer.Generate(GeneratePadding,3f,30f);
        
        if (result == null) return null;

        var img = Image.LoadPixelData<Rgba32>(result.Data, result.Width, result.Height);
        img.Mutate(i => i.Pad(result.Width + (AtlasPadding * 2), result.Height + (AtlasPadding * 2),new SixLabors.ImageSharp.Color(new Rgba32(255,255,255,255))));
        return new GeneratedMsdf()
        {
            Character = c,
            Msdf = img
        };
    }

    public async Task<MtsdfFont> GenerateFont(float size)
    {
        const int rectSize = 512;
        
        var font = _family.CreateFont(size);
        
        var msdfs = (await Task.WhenAll(Enumerable.Range('\x1', 127).Select(i => (char)i).Where(c => !char.IsControl(c)).Select(c => Task.Run(() => GenerateMsdf(c,font))))).Where(c => c != null).Select(c => (GeneratedMsdf)c!).ToArray();

        List<Packer> packers = [new Packer(rectSize, rectSize)];
        
        for(var i = 0; i < msdfs.Length; i++)
        {
            var msdf = msdfs[i];
            
            var targetPacker = packers.Last();
            if (targetPacker.PackRect(msdf.Msdf.Width, msdf.Msdf.Height, msdf) == null)
            {
                packers.Add(new Packer(rectSize,rectSize));
                i--;
            }
        }
        
        

        var atlases = packers.Select(c => new Image<Rgba32>(rectSize,rectSize, new Rgba32(0, 0, 0, 0))).ToArray();

        List<MsdfAtlasChar> atlasCharacters = [];
        for (var i = 0; i < packers.Count; i++)
        {
            var packer = packers[i];
            
            var atlas = atlases[i];
            atlas.Mutate(o =>
            {
                foreach (var rect in packer.PackRectangles)
                {
                    var generated = (GeneratedMsdf)rect.Data;
                    atlasCharacters.Add(new MsdfAtlasChar
                    {
                        Id = generated.Character,
                        AtlasIdx = i,
                        X = rect.X + AtlasPadding + GeneratePadding,
                        Y = rect.Y + AtlasPadding + GeneratePadding,
                        Height =  rect.Height - (AtlasPadding * 2 + GeneratePadding * 2),
                        Width = rect.Width  - (AtlasPadding * 2 + GeneratePadding * 2)
                    });

                    o.DrawImage(generated.Msdf, new Point(rect.X, rect.Y), 1F);
                }
            });
            
            //await atlas.SaveAsPngAsync($"./atlas_{i}.png");
        }
        
        return new MtsdfFont(_family,atlases.Select((c,idx) => new Texture(c.ToBytes(),new VkExtent3D()
        {
            width = (uint)c.Width,
            height = (uint)c.Height,
            depth = 1
        },EImageFormat.Rgba8UNorm,EImageFilter.Linear,EImageTiling.ClampEdge,false,$"{_family.Name} Atlas {idx}")).ToArray(),atlasCharacters);
    }
}
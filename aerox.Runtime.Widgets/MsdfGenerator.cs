using System.Runtime.InteropServices;
using aerox.Runtime.Graphics;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using StbRectPackSharp;
using TerraFX.Interop.Vulkan;

namespace aerox.Runtime.Widgets;

public class MsdfGenerator(FontFamily family)
{
    private struct GeneratedMsdf(Image msdf, char character)
    {
        public Image Msdf = msdf;
        public char Character = character;
    }
    
    private readonly FontFamily _family = family;
    private readonly Mutex mutex = new Mutex();
    private GeneratedMsdf? GenerateMsdf(char c, Font font)
    {
        var msdfRenderer = new MsdfRenderer();
        
        lock (mutex)
        {
            TextRenderer.RenderTextTo(msdfRenderer, $"{c}", new TextOptions(font));
        }
        
        Image? result = null;
        msdfRenderer.Generate((data, width, height, dataSize) =>
        {
            var copiedData = new byte[dataSize];
            Marshal.Copy(data, copiedData, 0, (int)dataSize);
            
            result = Image.LoadPixelData<Rgba32>(copiedData, (int)width, (int)height);
            
            // msdfFont.Add(c, new Texture(copiedData, new VkExtent3D
            //     {
            //         width = width,
            //         height = height,
            //         depth = 1
            //     }, ImageFormat.Rgba8, ImageFilter.Linear, ImageTiling.ClampEdge, false, $"msdf => {c}"));
        });
        msdfRenderer.Dispose();
        if (result == null) return null;
        return new GeneratedMsdf()
        {
            Character = c,
            Msdf = result
        };
    }

    public async Task<MsdfFont> GenerateFont(float size)
    {
        const int rectSize = 512;
        
        var font = _family.CreateFont(size);
        
        var chars = Enumerable.Range(32, 127).Select(i => (char)i).ToArray();

        var msdfs = (await Task.WhenAll(chars.Select(c => Task.Run(() => GenerateMsdf(c,font))))).Where(c => c != null).Select(c => (GeneratedMsdf)c!).ToArray();

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
                        X = rect.X,
                        Y = rect.Y,
                        Height =  rect.Height,
                        Width = rect.Width
                    });

                    o.DrawImage(generated.Msdf, new Point(rect.X, rect.Y), 1F);
                }
            });
            
            //await atlas.SaveAsPngAsync($@"F:\atlas_{i}.png");
        }
        
        return new MsdfFont(_family,atlases.Select((c,idx) => new Texture(c.ToBytes(),new VkExtent3D()
        {
            width = (uint)c.Width,
            height = (uint)c.Height,
            depth = 1
        },ImageFormat.Rgba32,ImageFilter.Linear,ImageTiling.ClampEdge,false,$"{_family.Name} Atlas {idx}")).ToArray(),atlasCharacters);
    }
}
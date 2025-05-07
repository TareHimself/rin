using Rin.Engine.Graphics;
using Rin.Engine.Graphics.Textures;
using SixLabors.Fonts;

namespace Rin.Engine.Views.Sdf;

public class MtsdfFont : Reservable
{
    private readonly TextureHandle[] _atlases;
    private readonly FontFamily _fontFamily;
    private readonly Mutex _mutex = new();
    private readonly Dictionary<string, SdfVector> _vectors;

    public MtsdfFont(FontFamily fontFamily, TextureHandle[] atlases, Dictionary<string, SdfVector> vectors)
    {
        _atlases = atlases;
        _vectors = vectors;
        _fontFamily = fontFamily;
    }

    public FontFamily GetFontFamily()
    {
        return _fontFamily;
    }

    protected override void OnDispose(bool isManual)
    {
        SGraphicsModule.Get().GetTextureFactory().FreeTextures(_atlases);
    }

    public TextureHandle GetAtlasTextureId(int atlasId)
    {
        return _atlases[atlasId];
    }

    public TextureHandle[] GetAtlases()
    {
        return _atlases;
    }

    public SdfVector? GetGlyphInfo(int glyphId)
    {
        return _vectors.GetValueOrDefault(glyphId.ToString());
    }
}
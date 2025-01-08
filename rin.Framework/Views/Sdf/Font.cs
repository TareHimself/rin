using rin.Framework.Core;
using rin.Framework.Graphics;
using SixLabors.Fonts;

namespace rin.Framework.Views.Sdf;

public class Font : Reservable
{
    private readonly Mutex _mutex = new();
    private readonly FontFamily _fontFamily;
    private readonly int[] _atlases;
    private readonly Dictionary<string,SdfVector> _vectors;

    public Font(FontFamily fontFamily,int[] atlases,Dictionary<string,SdfVector> vectors)
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
        SGraphicsModule.Get().GetTextureManager().FreeTextures(_atlases);
    }

    public int GetAtlasTextureId(int atlasId)
    {
        return _atlases[atlasId];
    }
    
    public int[] GetAtlases()
    {
        return _atlases;
    }

    public SdfVector? GetGlyphInfo(int glyphId) => _vectors.GetValueOrDefault(glyphId.ToString());
}
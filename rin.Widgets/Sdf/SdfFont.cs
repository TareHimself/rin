using rin.Core;
using rin.Graphics;
using SixLabors.Fonts;

namespace rin.Widgets.Sdf;

public class SdfFont : MultiDisposable
{
    private readonly Mutex _mutex = new();
    private readonly FontFamily _fontFamily;
    private readonly int[] _atlases;
    private readonly Dictionary<string,SdfVector> _vectors;

    public SdfFont(FontFamily fontFamily,int[] atlases,Dictionary<string,SdfVector> vectors)
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
        SGraphicsModule.Get().GetResourceManager().FreeTextures(_atlases);
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
using Rin.Framework.Graphics;
using Rin.Framework.Graphics.Textures;
using SixLabors.Fonts;

namespace Rin.Framework.Views.Sdf;

public class MtsdfFont : Reservable
{
    private readonly ImageHandle[] _atlases;
    private readonly FontFamily _fontFamily;
    private readonly Mutex _mutex = new();
    private readonly Dictionary<string, SdfVector> _vectors;

    public MtsdfFont(FontFamily fontFamily, ImageHandle[] atlases, Dictionary<string, SdfVector> vectors)
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
        SGraphicsModule.Get().GetImageFactory().FreeHandles(_atlases);
    }

    public ImageHandle GetAtlasTextureId(int atlasId)
    {
        return _atlases[atlasId];
    }

    public ImageHandle[] GetAtlases()
    {
        return _atlases;
    }

    public SdfVector? GetGlyphInfo(int glyphId)
    {
        return _vectors.GetValueOrDefault(glyphId.ToString());
    }
}
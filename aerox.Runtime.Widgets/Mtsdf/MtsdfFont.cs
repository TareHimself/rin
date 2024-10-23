using aerox.Runtime.Graphics;
using SixLabors.Fonts;

namespace aerox.Runtime.Widgets.Mtsdf;

public class MtsdfFont : MultiDisposable
{
    private readonly Mutex _mutex = new();
    private readonly Dictionary<int, int> _characterMap = new();
    private readonly int[] _atlases;
    private readonly List<MtsdfAtlasGlyph> _glyphs;
    private readonly FontFamily _fontFamily;

    public MtsdfFont(FontFamily fontFamily,int[] atlases,List<MtsdfAtlasGlyph> glyphs)
    {
        _atlases = atlases;
        _glyphs = glyphs;
        _fontFamily = fontFamily;
        for(var i = 0; i < _glyphs.Count; i++)
        {
            _characterMap.Add(_glyphs[i].Id,i);
        }
    }

    public FontFamily GetFontFamily()
    {
        return _fontFamily;
    }

    protected override void OnDispose(bool isManual)
    {
        lock (_mutex)
        {
            SGraphicsModule.Get().GetResourceManager().FreeTextures(_atlases);
            _characterMap.Clear();
            _mutex.Dispose();
        }
    }

    public int GetAtlasTextureId(int atlasId)
    {
        return _atlases[atlasId];
    }
    
    public int[] GetAtlases()
    {
        return _atlases;
    }

    public MtsdfAtlasGlyph? GetGlyphInfo(int glyphId)
    {
        lock (_mutex)
        {
            return _characterMap.TryGetValue(glyphId, out var index) ? _glyphs[index] : null;
        }
    }
}
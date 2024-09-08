using aerox.Runtime.Graphics;
using SixLabors.Fonts;

namespace aerox.Runtime.Widgets.Mtsdf;

public class MtsdfFont : MultiDisposable
{
    private readonly Mutex _mutex = new();
    private readonly Dictionary<int, int> _characterMap = new();
    private readonly Texture[] _atlases;
    private readonly List<MtsdfAtlasGlyph> _glyphs;
    private readonly FontFamily _fontFamily;

    public MtsdfFont(FontFamily fontFamily,Texture[] atlases,List<MtsdfAtlasGlyph> glyphs)
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
            foreach (var tex in _atlases) tex.Dispose();
            _characterMap.Clear();
            _mutex.Dispose();
        }
    }

    public Texture[] GetAtlases()
    {
        return _atlases.ToArray();
    }

    public MtsdfAtlasGlyph? GetGlyphInfo(int glyphId)
    {
        lock (_mutex)
        {
            return _characterMap.TryGetValue(glyphId, out var index) ? _glyphs[index] : null;
        }
    }
}
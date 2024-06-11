using aerox.Runtime.Graphics;
using SixLabors.Fonts;

namespace aerox.Runtime.Widgets;

public class MtsdfFont : MultiDisposable
{
    private readonly Mutex _mutex = new();
    private readonly Dictionary<int, int> _characterMap = new();
    private readonly Texture[] _atlases;
    private readonly List<MsdfAtlasChar> _chars;
    private readonly FontFamily _fontFamily;

    public MtsdfFont(FontFamily fontFamily,Texture[] atlases,List<MsdfAtlasChar> chars)
    {
        _atlases = atlases;
        _chars = chars;
        _fontFamily = fontFamily;
        for(var i = 0; i < _chars.Count; i++)
        {
            _characterMap.Add(_chars[i].Id,i);
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

    public MsdfAtlasChar? GetCharInfo(int code)
    {
        lock (_mutex)
        {
            if (_characterMap.TryGetValue(code, out var index)) return _chars[index];

            return null;
        }
    }
}
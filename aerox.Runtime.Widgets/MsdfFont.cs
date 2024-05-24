using aerox.Runtime.Graphics;
using SixLabors.Fonts;

namespace aerox.Runtime.Widgets;

public class MsdfFont(FontFamily fontFamily) : MultiDisposable
{
    private readonly Mutex _mutex = new();
    private readonly Dictionary<int, int> _textureLookup = new();
    private readonly List<Texture> _textures = new();

    public FontFamily GetFontFamily()
    {
        return fontFamily;
    }

    public void Add(int code, Texture texture)
    {
        lock (_mutex)
        {
            _textureLookup.Add(code, _textures.Count);
            _textures.Add(texture);
        }
    }

    protected override void OnDispose(bool isManual)
    {
        lock (_mutex)
        {
            foreach (var tex in _textures) tex.Dispose();

            _textures.Clear();
            _textureLookup.Clear();
            _mutex.Dispose();
        }
    }

    public Texture[] GetTextures()
    {
        return _textures.ToArray();
    }

    public int? GetTextureIndex(int code)
    {
        lock (_mutex)
        {
            if (_textureLookup.TryGetValue(code, out var index)) return index;

            return null;
        }
    }
}
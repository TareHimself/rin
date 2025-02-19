using System.Collections.Concurrent;
using System.Numerics;
using rin.Framework.Core;
using rin.Framework.Core.Extensions;
using rin.Framework.Graphics;
using rin.Framework.Views.Sdf;
using SixLabors.Fonts;

namespace rin.Framework.Views.Font;

public class DefaultFontManager : IFontManager
{
    private const float RenderSize = 32.0f;
    private readonly ConcurrentDictionary<CacheKey, GlyphInfo> _atlases = [];
    private readonly CancellationTokenSource _cancellationSource = new();

    private readonly FontCollection _collection = new();

    private readonly GlyphInfo _defaultGlyph = new()
    {
        AtlasId = -1,
        State = GlyphState.Invalid,
        Size = Vector2.Zero,
        Coordinate = Vector4.Zero
    };

    public Task Prepare(FontFamily fontFamily, ReadOnlySpan<char> characters, FontStyle style = FontStyle.Regular)
    {
        var font = fontFamily.CreateFont(RenderSize, style);
        List<Pair<char, CacheKey>> toGenerate = [];
        foreach (var character in characters)
        {
            var key = new CacheKey(font.IsBold, font.IsItalic, character, fontFamily.Name);
            if (_atlases.ContainsKey(key)) continue;
            toGenerate.Add(new Pair<char, CacheKey>(character, key));
        }

        if (toGenerate.Empty()) return Task.CompletedTask;
        var pending = _defaultGlyph.Mutate(c =>
        {
            c.State = GlyphState.Pending;
            return c;
        });
        foreach (var (_, key) in toGenerate) _atlases.AddOrUpdate(key, pending, (k, i) => pending);

        return Task.Run(() =>
        {
            try
            {
                var options = new TextOptions(font);
                var renderers = toGenerate.Select(c => new MtsdfTextRenderer()).ToArray();
                for (var i = 0; i < toGenerate.Count; i++)
                    TextRenderer.RenderTextTo(renderers[i], [toGenerate[i].First], options);

                var results = (IEnumerable<Pair<Result, int>>)renderers
                    .Select((c, idx) => new Pair<Result?, int>(c.Generate(3f, GetPixelRange()), idx))
                    .Where(c => c.First != null);
                var textureManager = SGraphicsModule.Get().GetTextureManager();
                //var textureManager = SGraphicsModule.Get().GetTextureManager();
                var generated = results.Select(c =>
                {
                    var (result, index) = c;
                    var data = result.Data;
                    var size = new Vector2((float)result.Width, (float)result.Height);
                    var glyph = new GlyphInfo
                    {
                        AtlasId = 0,
                        State = GlyphState.Ready,
                        Size = size,
                        Coordinate = new Vector4(0.0f, 0.0f, size.X / result.PixelWidth, size.Y / result.PixelHeight)
                    };
                    return textureManager.CreateTexture(data,
                        new Extent3D((uint)result.PixelWidth, (uint)result.PixelHeight), ImageFormat.RGBA8,
                        tiling: ImageTiling.ClampEdge).Then(
                        id =>
                        {
                            glyph.AtlasId = id;
                            data.Dispose();
                            return new Pair<int, GlyphInfo>(index, glyph);
                        });
                }).WaitAll();

                foreach (var (index, glyph) in generated)
                    _atlases.AddOrUpdate(toGenerate[index].Second, glyph, (k, i) => glyph);
                // var codepoint = glyph.GlyphMetrics.CodePoint;
                // var targetStr = codepoint.ToString();
                // SixLaborsTextRenderer.RenderTextTo(mtsdfRenderer,targetStr,options);
                //
                // var result = mtsdfRenderer.Generate(3f,pixelRange);
                //
                // if (result == null) return null;
                //
                // Span<byte> data = result.Data;
                //
                // var img = Image.LoadPixelData<Rgba32>(data, result.PixelWidth, result.PixelHeight);
            }
            catch (Exception e)
            {
                foreach (var (_, key) in toGenerate) _atlases.Remove(key, out var _);
                Console.WriteLine(e);
                throw;
            }
        }, _cancellationSource.Token);
    }

    public void LoadSystemFonts()
    {
        _collection.AddSystemFonts();
    }

    public void LoadFont(Stream fileStream)
    {
        _collection.Add(fileStream);
    }

    public GlyphInfo GetGlyph(SixLabors.Fonts.Font font, char character)
    {
        var key = new CacheKey(font.IsBold, font.IsItalic, character, font.Family.Name);
        return _atlases.GetValueOrDefault(key, _defaultGlyph);
    }

    public bool TryGetFont(string name, out FontFamily family)
    {
        return _collection.TryGet(name, out family);
    }

    public void Dispose()
    {
        _cancellationSource.Cancel();
        SGraphicsModule.Get().GetTextureManager()
            .FreeTextures(_atlases.Select(c => c.Value.AtlasId).Where(c => c != -1).ToArray());
        _atlases.Clear();
    }

    public float GetPixelRange()
    {
        return 12.0f;
    }

    private readonly struct CacheKey(bool isBold, bool isItalic, char character, string fontName)
        : IEquatable<CacheKey>
    {
        private readonly bool _isBold = isBold;
        private readonly bool _isItalic = isItalic;
        private readonly char _character = character;
        private readonly string _fontName = fontName;

        public bool Equals(CacheKey other)
        {
            return _isBold == other._isBold && _isItalic == other._isItalic && _character == other._character &&
                   _fontName == other._fontName;
        }

        public override bool Equals(object? obj)
        {
            return obj is CacheKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(_isBold, _isItalic, _character, _fontName);
        }
    }
}
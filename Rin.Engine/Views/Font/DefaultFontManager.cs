using System.Collections.Concurrent;
using System.Numerics;
using System.Security.Cryptography;
using Rin.Engine.Core;
using Rin.Engine.Graphics;
using Rin.Engine.Views.Sdf;
using Rin.Engine.Core.Extensions;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Rin.Engine.Views.Font;

public class DefaultFontManager(IExternalFontCache? externalCache = null) : IFontManager
{
    private IExternalFontCache? _externalCache = externalCache;
    private const float RenderSize = 32.0f;
    private readonly ConcurrentDictionary<CacheKey, LiveGlyphInfo> _atlases = [];
    private readonly CancellationTokenSource _cancellationSource = new();
    private readonly BackgroundTaskQueue _backgroundTaskQueue = new();

    private readonly FontCollection _collection = new();

    private readonly LiveGlyphInfo _defaultLiveGlyph = new()
    {
        AtlasId = -1,
        State = LiveGlyphState.Invalid,
        Size = Vector2.Zero,
        Coordinate = Vector4.Zero
    };

    private SdfResult? GenerateGlyph(char character,SixLabors.Fonts.Font font,CacheKey cacheKey)
    {
        if (_externalCache?.Get(cacheKey.GetHashCode()) is {} data)
        {
            var result = new SdfResult();
            data.Read(result);
            data.Dispose();
            return result;
        }

        {
            var options = new TextOptions(font);
            using var renderer = new MtsdfTextRenderer();
            TextRenderer.RenderTextTo(renderer, [character], options);
            var result = renderer.Generate(3f, GetPixelRange());
            
            if (_externalCache is { SupportsSet: true } && result != null)
            {
                var stream = new MemoryStream();
                stream.Write(result);
                _externalCache.Set(cacheKey.GetHashCode(), stream);
            }

            return result;
        }
    }
    
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
        var pending = _defaultLiveGlyph.Mutate(c =>
        {
            c.State = LiveGlyphState.Pending;
            return c;
        });
        foreach (var (_, key) in toGenerate) _atlases.AddOrUpdate(key, pending, (k, i) => pending);
        
        
        return Task.Run(() =>
        {
            try
            {
                var results = toGenerate
                    .Select((c, idx) => new Pair<SdfResult?, int>(GenerateGlyph(c.First,font,c.Second), idx))
                    .Where(c => c.First != null);
                var textureManager = SGraphicsModule.Get().GetTextureManager();
                //var textureManager = SGraphicsModule.Get().GetTextureManager();
                var generated = results.Select(c =>
                {
                    var (result, index) = c;
                    var data = result.Data;
                    var size = new Vector2((float)result.Width, (float)result.Height);
                    var glyph = new LiveGlyphInfo
                    {
                        AtlasId = 0,
                        State = LiveGlyphState.Ready,
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
                            return new Pair<int, LiveGlyphInfo>(index, glyph);
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

    public LiveGlyphInfo GetGlyph(SixLabors.Fonts.Font font, char character)
    {
        var key = new CacheKey(font.IsBold, font.IsItalic, character, font.Family.Name);
        return _atlases.GetValueOrDefault(key, _defaultLiveGlyph);
    }

    public bool TryGetFont(string name, out FontFamily family)
    {
        return _collection.TryGet(name, out family);
    }

    public void Dispose()
    {
        _cancellationSource.Cancel();
        _backgroundTaskQueue.Dispose();
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
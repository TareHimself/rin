using System.Collections.Concurrent;
using System.Diagnostics;
using System.Numerics;
using Rin.Engine.Extensions;
using Rin.Engine.Graphics;
using Rin.Engine.Graphics.Textures;
using Rin.Engine.Views.Sdf;
using SixLabors.Fonts;

namespace Rin.Engine.Views.Font;

public class DefaultFontManager : IFontManager
{
    private const float RenderSize = 32.0f;
    private const float PixelRange = 12.0f;
    private readonly ConcurrentDictionary<CacheKey, LiveGlyphInfo> _atlases = [];
    private readonly BackgroundTaskQueue _backgroundTaskQueue = new();
    private readonly CancellationTokenSource _cancellationSource = new();
    private readonly FontCollection _collection = new();

    private readonly LiveGlyphInfo _defaultLiveGlyph = new()
    {
        AtlasHandle = ImageHandle.InvalidImage,
        State = LiveGlyphState.Invalid,
        Size = Vector2.Zero,
        Coordinate = Vector4.Zero
    };

    private readonly IExternalFontCache? _externalCache;
    private readonly ConcurrentDictionary<FontFamily, IFont> _fonts = [];

    public DefaultFontManager(IExternalFontCache? externalCache = null)
    {
        _externalCache = externalCache;
    }

    public Task Prepare(IFont font, IEnumerable<char> characters)
    {
        if (font is SixLaborsFont asFont)
        {
            var actualFont = asFont.Family.CreateFont(RenderSize, FontStyle.Regular);
            List<Pair<char, CacheKey>> toGenerate = [];
            foreach (var character in characters)
            {
                var key = new CacheKey(character, asFont.Name);
                if (_atlases.ContainsKey(key)) continue;
                toGenerate.Add(new Pair<char, CacheKey>(character, key));
            }

            if (toGenerate.Empty()) return Task.CompletedTask;


            var pending = _defaultLiveGlyph with
            {
                State = LiveGlyphState.Pending
            };
            foreach (var (_, key) in toGenerate) _atlases.AddOrUpdate(key, pending, (_, _) => pending);


            return Task.Run(() =>
            {
                try
                {
                    var results = toGenerate
                        .Select((c, idx) =>
                            new Pair<SdfResult?, int>(GenerateGlyph(c.First, actualFont, c.Second), idx))
                        .Where(c => c.First != null);
                    //var textureManager = SGraphicsModule.Get().GetTextureManager();
                    var generated = results.Select(c =>
                    {
                        var (result, index) = c;
                        Debug.Assert(result != null, nameof(result) + " != null");
                        using var data = result.Data;
                        var size = new Vector2((float)result.Width, (float)result.Height);
                        var glyph = new LiveGlyphInfo
                        {
                            AtlasHandle = ImageHandle.InvalidImage,
                            State = LiveGlyphState.Ready,
                            Size = size,
                            Coordinate = new Vector4(0.0f, 0.0f, size.X / result.PixelWidth,
                                size.Y / result.PixelHeight)
                        };

                        glyph.AtlasHandle = SGraphicsModule.Get().GetImageFactory().CreateTexture(result.Data.Copy(),
                            new Extent3D((uint)result.PixelWidth, (uint)result.PixelHeight), ImageFormat.RGBA8).handle;
                        return new Pair<int, LiveGlyphInfo>(index, glyph);
                    });

                    foreach (var (index, glyph) in generated)
                        _atlases.AddOrUpdate(toGenerate[index].Second, glyph, (_, _) => glyph);
                }
                catch (Exception e)
                {
                    foreach (var (_, key) in toGenerate) _atlases.Remove(key, out _);
                    Console.WriteLine(e);
                    throw;
                }
            }, _cancellationSource.Token);
        }

        return Task.FromException(new Exception("Unknown font class"));
    }

    public Task PrepareAtlas(IFont font, IEnumerable<char> characters)
    {
        if (font is SixLaborsFont asFont)
        {
            var actualFont = asFont.Family.CreateFont(RenderSize);
            List<Pair<char, CacheKey>> toGenerate = [];
            foreach (var character in characters)
            {
                var key = new CacheKey(character, asFont.Name);
                if (_atlases.ContainsKey(key)) continue;
                toGenerate.Add(new Pair<char, CacheKey>(character, key));
            }

            if (toGenerate.Empty()) return Task.CompletedTask;

            var pending = _defaultLiveGlyph with
            {
                State = LiveGlyphState.Pending
            };
            foreach (var (_, key) in toGenerate) _atlases.AddOrUpdate(key, pending, (_, _) => pending);
            return Task.Run(() =>
            {
                try
                {
                    var initialResults = Enumerable.Range(0, toGenerate.Count).Select(SdfResult? (_) => null).ToArray();

                    var rangePartitioner = Partitioner.Create(0, toGenerate.Count);

                    Parallel.ForEach(rangePartitioner, range =>
                    {
                        for (var i = range.Item1; i < range.Item2; i++)
                        {
                            var item = toGenerate[i];
                            initialResults[i] = GenerateGlyph(item.First, actualFont, item.Second);
                        }
                    });

                    var results = initialResults.Select((c, idx) => new Pair<SdfResult, int>(c, idx))
                        .Where(c => c.First != null).ToArray();

                    var atlasSize = 512;
                    var padding = 2;
                    List<RectPacker<Pair<SdfResult, int>>> packers = [new(atlasSize, atlasSize, padding)];

                    for (var i = 0; i < results.Length; i++)
                    {
                        var mtsdf = results[i];

                        var targetPacker = packers.Last();

                        if (targetPacker.Pack(mtsdf.First.PixelWidth, mtsdf.First.PixelHeight, mtsdf)) continue;

                        packers.Add(new RectPacker<Pair<SdfResult, int>>(atlasSize, atlasSize, padding));

                        i--;
                    }

                    var atlases = packers
                        .Select(p => HostImage.Create(new Extent2D(p.Width, p.Height), ImageFormat.RGBA8))
                        .ToArray();

                    // Write glyphs to images
                    for (var i = 0; i < packers.Count; i++)
                    {
                        var packer = packers[i];

                        using var atlas = atlases[i];
                        atlases[i] = atlas.Mutate(o =>
                        {
                            o.Fill(255, 255, 255, 0);
                            foreach (var rect in packer.Rects)
                            {
                                var generated = rect.Data;
                                using var img = HostImage.Create(generated.First.Data, (uint)generated.First.PixelWidth,
                                    (uint)generated.First.PixelHeight, 4);
                                generated.First.Data.Dispose();
                                o.DrawImage(img, new Offset2D(rect.X, rect.Y));
                                //o.DrawImage(img, new Point(rect.X, rect.Y), 1F);
                            }
                        });

                        //atlas.Save(File.OpenWrite($"./atlas{i}.png"));
                    }

                    // Upload textures to gpu
                    var atlasIds = atlases.Select(c => c.CreateTexture(tiling: ImageTiling.ClampEdge).handle).ToArray();

                    // Update Live glyphs
                    for (var i = 0; i < packers.Count; i++)
                    {
                        var packer = packers[i];

                        foreach (var rect in packer.Rects)
                        {
                            var generated = rect.Data;
                            var pt1 = new Vector2(rect.X, rect.Y);
                            var pixelSize = new Vector2(generated.First.PixelWidth, generated.First.PixelHeight);
                            var pt2 = pt1 + pixelSize;
                            var pt1Coord = pt1 / atlasSize;
                            var pt2Coord = pt2 / atlasSize;
                            var glyph = new LiveGlyphInfo
                            {
                                AtlasHandle = atlasIds[i],
                                State = LiveGlyphState.Ready,
                                Size = pixelSize,
                                Coordinate = new Vector4(pt1Coord, pt2Coord.X, pt2Coord.Y)
                            };
                            _atlases.AddOrUpdate(toGenerate[generated.Second].Second, glyph, (_, _) => glyph);
                        }
                    }

                    foreach (var atlas in atlases) atlas.Dispose();
                }
                catch (Exception e)
                {
                    foreach (var (_, key) in toGenerate) _atlases.Remove(key, out _);
                    Console.WriteLine(e);
                    throw;
                }
            }, _cancellationSource.Token);
        }

        return Task.FromException(new Exception("Unknown font class"));
    }

    public void LoadSystemFonts()
    {
        _collection.AddSystemFonts();
    }

    public void LoadFont(Stream fileStream)
    {
        _collection.Add(fileStream);
    }

    public LiveGlyphInfo GetGlyph(IFont font, char character)
    {
        var key = new CacheKey(character, font.Name);
        return _atlases.GetValueOrDefault(key, _defaultLiveGlyph);
    }

    public IFont? GetFont(string name)
    {
        if (_collection.TryGet(name, out var family))
        {
            if (_fonts.TryGetValue(family, out var font)) return font;
            var insert = new SixLaborsFont(family, this);
            _fonts.AddOrUpdate(family, insert, (k, i) => insert);
            return insert;
        }

        return null;
    }

    public GlyphRect[] MeasureText(IFont font, in ReadOnlySpan<char> text, float size,
        float maxWidth = float.PositiveInfinity)
    {
        Debug.Assert(font is SixLaborsFont);
        var myFont = (SixLaborsFont)font;
        var bounds = GetCharacterBounds(myFont, text, size, maxWidth);

        return bounds.ToArray().Select(c => new GlyphRect
        {
            Character = c.Codepoint.ToString().First(),
            Position = new Vector2(c.Bounds.X, c.Bounds.Y),
            Size = new Vector2(c.Bounds.Width, c.Bounds.Height)
        }).ToArray();
    }

    public void Dispose()
    {
        _cancellationSource.Cancel();
        _backgroundTaskQueue.Dispose();
        SGraphicsModule.Get().GetImageFactory()
            .FreeHandles(_atlases.Select(c => c.Value.AtlasHandle).Where(c => c.Id >= 0).ToArray());
        _atlases.Clear();
    }

    public float GetPixelRange()
    {
        return PixelRange;
    }

    private GlyphBounds[] GetCharacterBounds(SixLaborsFont font, in ReadOnlySpan<char> text, float size,
        float maxWidth = float.PositiveInfinity)
    {
        // var key = new StringBuilder().Append(size.ToString(CultureInfo.InvariantCulture)).Append('|').Append(maxWidth.ToString(CultureInfo.InvariantCulture)).Append('|').Append(text).ToString();
        // {
        //     if(_boundsCache.TryGetValue(key, out var bounds)) return bounds;
        // }
        {
            var actualFont = font.Family.CreateFont(size);

            var opts = new TextOptions(actualFont)
            {
                WrappingLength = maxWidth < float.PositiveInfinity ? maxWidth : -1
            };

            TextMeasurer.TryMeasureCharacterBounds(text, opts, out var boundsSpan);

            var bounds = boundsSpan.ToArray();

            // _boundsCache[key] = bounds;

            return bounds;
        }
    }

    private SdfResult? GenerateGlyph(char character, SixLabors.Fonts.Font font, CacheKey cacheKey)
    {
        if (_externalCache?.Get(cacheKey.GetHashCode()) is { } data)
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

    public bool TryGetFont(string name, out FontFamily family)
    {
        return _collection.TryGet(name, out family);
    }

    private readonly struct CacheKey(char character, string fontName)
        : IEquatable<CacheKey>
    {
        private readonly char _character = character;
        private readonly string _fontName = fontName;

        public bool Equals(CacheKey other)
        {
            return _character == other._character &&
                   _fontName == other._fontName;
        }

        public override bool Equals(object? obj)
        {
            return obj is CacheKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(_character, _fontName);
        }
    }
}
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Numerics;
using Rin.Framework.Extensions;
using Rin.Framework.Graphics;
using Rin.Framework.Graphics.Textures;
using Rin.Framework.Views.Sdf;
using SixLabors.Fonts;

namespace Rin.Framework.Views.Font;

public class DefaultFontManager : IFontManager
{
    private const float RenderSize = 32.0f;
    private const float PixelRange = 12.0f;
    private readonly ConcurrentDictionary<CacheKey, LiveGlyphInfo> _atlases = [];
    private readonly BackgroundTaskQueue _backgroundTaskQueue = new();

    private readonly ISdfCache? _cache;
    private readonly CancellationTokenSource _cancellationSource = new();
    private readonly FontCollection _collection = new();

    private readonly LiveGlyphInfo _defaultLiveGlyph = new()
    {
        AtlasHandle = ImageHandle.InvalidImage,
        State = LiveGlyphState.Invalid,
        Size = Vector2.Zero,
        Coordinate = Vector4.Zero
    };

    private readonly ConcurrentDictionary<FontFamily, IFont> _fonts = [];

    public DefaultFontManager(ISdfCache? cache = null)
    {
        _cache = SApplication.Provider.AddSingle<ISdfCache>(new DiskSdfCache(Path.Combine(SApplication.Directory, "sdfs.bin")));
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
                        using var data = result.Image;
                        var size = new Vector2((float)result.Width, (float)result.Height);
                        var glyph = new LiveGlyphInfo
                        {
                            AtlasHandle = ImageHandle.InvalidImage,
                            State = LiveGlyphState.Ready,
                            Size = size,
                            Coordinate = new Vector4(0.0f, 0.0f, size.X / result.Image.Extent.Width,
                                size.Y / result.Image.Extent.Height)
                        };

                        glyph.AtlasHandle = result.Image.CreateTexture().handle;
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
                    var initialResults = new SdfResult?[toGenerate.Count];

                    var rangePartitioner = Partitioner.Create(0, toGenerate.Count);

                    Parallel.ForEach(rangePartitioner, range =>
                    {
                        for (var i = range.Item1; i < range.Item2; i++)
                        {
                            var item = toGenerate[i];
                            initialResults[i] = GenerateGlyph(item.First, actualFont, item.Second);
                        }
                    });

                    List<Pair<SdfResult, int>> results = [];
                    for (var i = 0; i < toGenerate.Count; i++)
                    {
                        var initialResult = initialResults[i];
                        if(initialResult is null) continue;
                        results.Add(new Pair<SdfResult, int>(initialResult, i));
                    }

                    var atlasSize = 512;
                    var padding = 2;
                    List<RectPacker<Pair<SdfResult, int>>> packers = [new(atlasSize, atlasSize, padding)];

                    for (var i = 0; i < results.Count; i++)
                    {
                        var mtsdf = results[i];

                        var targetPacker = packers.Last();

                        if (targetPacker.Pack(mtsdf.First.Image.Extent, mtsdf)) continue;

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
                                o.DrawImage(rect.Data.First.Image, new Offset2D(rect.X, rect.Y));
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
                            var size = generated.First.Image.Extent;
                            var pt1 = new Vector2(rect.X, rect.Y);
                            var pixelSize = new Vector2(size.Width, size.Height);
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
                            rect.Data.First.Image.Dispose();
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
        var sdfCacheKey = $"Font/{cacheKey.FontName}/{cacheKey.Character}";

        if (_cache is not null && _cache.HasVector(sdfCacheKey))
        {
            var vector = _cache.GetVector(sdfCacheKey)!;
            var image = _cache.LoadImage(vector.ImageId)!;
            return new SdfResult(image, vector.Size.X, vector.Size.Y);
        }

        // {
        //     if (_cache?.GetVector($"{cacheKey.GetHashCode()}") is { } data&& _cache.LoadImage(data.AtlasIdx))
        //     {
        //         var result = new SdfResult()
        //         {
        //         
        //         }
        //         data.Read(result);
        //         data.Dispose();
        //         return result;
        //     }
        // }
        //
        {
            var options = new TextOptions(font);
            using var renderer = new MtsdfTextRenderer();
            TextRenderer.RenderTextTo(renderer, [character], options);
            var result = renderer.Generate(3f, GetPixelRange());

            if (result is not null && _cache is not null)
                if (!_cache.HasVector(sdfCacheKey))
                {
                    var imageId = _cache.AddImage(result.Image);
                    var actualSize = result.Image.Extent;

                    _cache.AddVector(new SdfVector
                    {
                        Id = sdfCacheKey,
                        ImageId = imageId,
                        Offset = Vector2.Zero,
                        Size = new Vector2((float)result.Width, (float)result.Height),
                        Coordinates = new Vector4(0f, 0f, actualSize.Width / (float)result.Image.Extent.Width,
                            actualSize.Height / (float)result.Image.Extent.Height),
                        PixelRange = PixelRange
                    });
                }

            // if (_cache is { SupportsSet: true } && result != null)
            // {
            //     var stream = new MemoryStream();
            //     stream.Write(result);
            //     _cache.Set(cacheKey.GetHashCode(), stream);
            // }
            //
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
        public readonly char Character = character;
        public readonly string FontName = fontName;

        public bool Equals(CacheKey other)
        {
            return Character == other.Character &&
                   FontName == other.FontName;
        }

        public override bool Equals(object? obj)
        {
            return obj is CacheKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Character, FontName);
        }
    }
}
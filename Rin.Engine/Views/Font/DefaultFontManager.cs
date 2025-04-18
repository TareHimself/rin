﻿using System.Collections.Concurrent;
using System.Diagnostics;
using System.Numerics;
using Rin.Engine.Extensions;
using Rin.Engine.Graphics;
using Rin.Engine.Views.Sdf;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Rin.Engine.Views.Font;

public class DefaultFontManager(IExternalFontCache? externalCache = null) : IFontManager
{
    private const float RenderSize = 32.0f;
    private readonly ConcurrentDictionary<CacheKey, LiveGlyphInfo> _atlases = [];
    private readonly BackgroundTaskQueue _backgroundTaskQueue = new();
    private readonly CancellationTokenSource _cancellationSource = new();
    private readonly FontCollection _collection = new();

    private readonly LiveGlyphInfo _defaultLiveGlyph = new()
    {
        AtlasId = -1,
        State = LiveGlyphState.Invalid,
        Size = Vector2.Zero,
        Coordinate = Vector4.Zero
    };

    private readonly IExternalFontCache? _externalCache = externalCache;
    private readonly ConcurrentDictionary<FontFamily, IFont> _fonts = [];

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
            foreach (var (_, key) in toGenerate) _atlases.AddOrUpdate(key, pending, (k, i) => pending);


            return Task.Run(() =>
            {
                try
                {
                    var results = toGenerate
                        .Select((c, idx) =>
                            new Pair<SdfResult?, int>(GenerateGlyph(c.First, actualFont, c.Second), idx))
                        .Where(c => c.First != null);
                    var textureManager = SGraphicsModule.Get().GetTextureFactory();
                    //var textureManager = SGraphicsModule.Get().GetTextureManager();
                    var generated = results.Select(c =>
                    {
                        var (result, index) = c;
                        Debug.Assert(result != null, nameof(result) + " != null");
                        using var data = result.Data;
                        var size = new Vector2((float)result.Width, (float)result.Height);
                        var glyph = new LiveGlyphInfo
                        {
                            AtlasId = 0,
                            State = LiveGlyphState.Ready,
                            Size = size,
                            Coordinate = new Vector4(0.0f, 0.0f, size.X / result.PixelWidth,
                                size.Y / result.PixelHeight)
                        };

                        glyph.AtlasId = SGraphicsModule.Get().CreateTexture(result.Data.Copy(),
                            new Extent3D((uint)result.PixelWidth, (uint)result.PixelHeight), ImageFormat.RGBA8,
                            tiling: ImageTiling.ClampEdge).First;
                        return new Pair<int, LiveGlyphInfo>(index, glyph);
                    });

                    foreach (var (index, glyph) in generated)
                        _atlases.AddOrUpdate(toGenerate[index].Second, glyph, (k, i) => glyph);
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
            foreach (var (_, key) in toGenerate) _atlases.AddOrUpdate(key, pending, (k, i) => pending);
            return Task.Run(() =>
            {
                try
                {
                    var initialResults = Enumerable.Range(0, toGenerate.Count).Select(SdfResult? (c) => null).ToArray();

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

                    var textureManager = SGraphicsModule.Get().GetTextureFactory();

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
                        .Select(p => new Image<Rgba32>(p.Width, p.Height, new Rgba32(255, 255, 255, 0)))
                        .ToArray();

                    List<Pair<int, LiveGlyphInfo>> generatedGlyphs = [];

                    // Write glyphs to images
                    for (var i = 0; i < packers.Count; i++)
                    {
                        var packer = packers[i];

                        var atlas = atlases[i];
                        atlas.Mutate(o =>
                        {
                            foreach (var rect in packer.Rects)
                            {
                                var generated = rect.Data;

                                Span<byte> data = generated.First.Data;

                                using var img = Image.LoadPixelData<Rgba32>(data, generated.First.PixelWidth,
                                    generated.First.PixelHeight);

                                generated.First.Data.Dispose();

                                o.DrawImage(img, new Point(rect.X, rect.Y), 1F);
                            }
                        });
                    }

                    // Upload textures to gpu
                    var atlasIds = atlases.Select(c => SGraphicsModule.Get().CreateTexture(c.ToBuffer(),
                        new Extent3D((uint)c.Width, (uint)c.Height), ImageFormat.RGBA8,
                        tiling: ImageTiling.ClampEdge).First).ToArray();

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
                                AtlasId = atlasIds[i],
                                State = LiveGlyphState.Ready,
                                Size = pixelSize,
                                Coordinate = new Vector4(pt1Coord, pt2Coord.X, pt2Coord.Y)
                            };
                            _atlases.AddOrUpdate(toGenerate[generated.Second].Second, glyph, (k, i) => glyph);
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
            if (_fonts.ContainsKey(family)) return _fonts[family];
            var insert = new SixLaborsFont(family);
            _fonts.AddOrUpdate(family, insert, (k, i) => insert);
            return insert;
        }

        return null;
    }

    public GlyphRect[] MeasureText(IFont font, ReadOnlySpan<char> text, float size,
        float maxWidth = float.PositiveInfinity)
    {
        if (font is SixLaborsFont asFont)
        {
            var actualFont = asFont.Family.CreateFont(size);

            var opts = new TextOptions(actualFont)
            {
                WrappingLength = maxWidth < float.PositiveInfinity ? maxWidth : -1
            };

            TextMeasurer.TryMeasureCharacterBounds(text, opts, out var bounds);

            return bounds.ToArray().Select(c => new GlyphRect
            {
                Character = c.Codepoint.ToString().First(),
                Position = new Vector2(c.Bounds.X, c.Bounds.Y),
                Size = new Vector2(c.Bounds.Width, c.Bounds.Height)
            }).ToArray();
        }

        throw new Exception("Unknown font type");
    }

    public void Dispose()
    {
        _cancellationSource.Cancel();
        _backgroundTaskQueue.Dispose();
        SGraphicsModule.Get().GetTextureFactory()
            .FreeTextures(_atlases.Select(c => c.Value.AtlasId).Where(c => c != -1).ToArray());
        _atlases.Clear();
    }

    public float GetPixelRange()
    {
        return 6.0f;
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
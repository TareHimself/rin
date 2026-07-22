using System.Collections.Concurrent;
using System.Diagnostics;
using System.Numerics;
using Rin.Core.Graphics;
using Rin.Core.Shared.Threading;
using Rin.Core.Views.Sdf;
using Rin.Core.Extensions;

namespace Rin.Core.Views.Font;

public class HarfBuzzFontManager : IFontManager
{
    private const float RenderSize = 32.0f;
    private const float PixelRange = 12.0f;
    private const int AtlasSize = 512;
    private const int AtlasPadding = 2;
    private readonly ConcurrentDictionary<CacheKey, LiveGlyphInfo> _atlases = [];
    private readonly BackgroundTaskQueue _backgroundTaskQueue = new()
    {
        Name = "HarfBuzzFontManager Task Queue"
    };

    private readonly ISdfCache? _cache;
    private readonly CancellationTokenSource _cancellationSource = new();
    private readonly IGraphicsModule _graphicsModule;

    private readonly LiveGlyphInfo _defaultLiveGlyph = new()
    {
        AtlasHandle = ResourceHandle.InvalidTexture,
        State = LiveGlyphState.Invalid,
        Size = Vector2.Zero,
        Coordinate = Vector4.Zero
    };

    /// <summary>
    ///     Resolved state for a character whose glyph has no ink (e.g. NBSP, zero-width joiners) - Ready with
    ///     nothing to draw, as opposed to Invalid/Pending which would tell callers to keep waiting on it.
    /// </summary>
    private readonly LiveGlyphInfo _emptyGlyph = new()
    {
        AtlasHandle = ResourceHandle.InvalidTexture,
        State = LiveGlyphState.Ready,
        Size = Vector2.Zero,
        Coordinate = Vector4.Zero
    };

    private readonly ConcurrentDictionary<string, HarfBuzzFont> _fonts = new(StringComparer.OrdinalIgnoreCase);

    public HarfBuzzFontManager(ISdfCache? cache = null, IGraphicsModule? graphicsModule = null)
    {
        _graphicsModule = graphicsModule ?? IGraphicsModule.Get();
        // _cache = Global.Provider.AddSingle<ISdfCache>(
        //     new DiskSdfCache(Path.Combine(Global.Directory, "sdfs.bin")));
    }

    public Task Prepare(IFont font, IEnumerable<char> characters)
    {
        if (font is HarfBuzzFont asFont)
        {
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
                    Parallel.For(0, toGenerate.Count,
                        i => initialResults[i] = GenerateGlyph(toGenerate[i].First, asFont, toGenerate[i].Second));

                    for (var index = 0; index < toGenerate.Count; index++)
                    {
                        var result = initialResults[index];
                        var key = toGenerate[index].Second;

                        if (result is null)
                        {
                            // No ink (e.g. NBSP, zero-width joiners) - resolve immediately instead of leaving
                            // this glyph stuck at Pending forever, which would otherwise permanently defeat
                            // TextBoxView's layout cache for any text containing it.
                            _atlases.AddOrUpdate(key, _emptyGlyph, (_, _) => _emptyGlyph);
                            continue;
                        }

                        using var data = result.Image;
                        var size = new Vector2((float)result.Width, (float)result.Height);
                        var glyph = new LiveGlyphInfo
                        {
                            AtlasHandle = ResourceHandle.InvalidTexture,
                            State = LiveGlyphState.Pending,
                            Size = size,
                            Coordinate = new Vector4(0.0f, 0.0f, size.X / result.Image.Extent.Width,
                                size.Y / result.Image.Extent.Height)
                        };
                        _atlases.AddOrUpdate(key, glyph, (_, _) => glyph);

                        result.Image.CreateTexture(out _, graphicsModule: _graphicsModule).Then(handle =>
                        {
                            if (!_atlases.TryGetValue(key, out var val)) return;
                            val = val with { State = LiveGlyphState.Ready, AtlasHandle = handle };
                            _atlases.AddOrUpdate(key, val, (_, _) => val);
                        });
                    }
                }
                catch
                {
                    foreach (var (_, key) in toGenerate) _atlases.Remove(key, out _);
                    throw;
                }
            }, _cancellationSource.Token);
        }

        return Task.FromException(new Exception("Unknown font class"));
    }

    public Task PrepareAtlas(IFont font, IEnumerable<char> characters)
    {
        if (font is HarfBuzzFont asFont)
        {
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
                    Parallel.For(0, toGenerate.Count,
                        i => initialResults[i] = GenerateGlyph(toGenerate[i].First, asFont, toGenerate[i].Second));

                    List<Pair<SdfResult, int>> results = [];
                    for (var i = 0; i < toGenerate.Count; i++)
                    {
                        var initialResult = initialResults[i];
                        if (initialResult is null)
                        {
                            // No ink (e.g. NBSP, zero-width joiners) - resolve immediately instead of leaving
                            // this glyph stuck at Pending forever, which would otherwise permanently defeat
                            // TextBoxView's layout cache for any text containing it.
                            var key = toGenerate[i].Second;
                            _atlases.AddOrUpdate(key, _emptyGlyph, (_, _) => _emptyGlyph);
                            continue;
                        }

                        results.Add(new Pair<SdfResult, int>(initialResult, i));
                    }

                    if (results.Empty()) return;

                    List<RectPacker<Pair<SdfResult, int>>> packers = [new(AtlasSize, AtlasSize, AtlasPadding)];

                    for (var i = 0; i < results.Count; i++)
                    {
                        var targetPacker = packers[^1];

                        if (targetPacker.Pack(results[i].First.Image.Extent, results[i])) continue;

                        packers.Add(new RectPacker<Pair<SdfResult, int>>(AtlasSize, AtlasSize, AtlasPadding));

                        i--;
                    }

                    // Write glyphs to images, update live glyphs, upload textures to the gpu
                    foreach (var packer in packers)
                    {
                        using var atlas = HostImage.Create(new Extent2D((uint)packer.Width, (uint)packer.Height),
                            ImageFormat.RGBA8);
                        var glyphsInAtlas = new List<CacheKey>();
                        var packedAtlas = atlas.Mutate(o =>
                        {
                            o.Fill(255, 255, 255, 0);
                            foreach (var rect in packer.Rects)
                            {
                                o.DrawImage(rect.Data.First.Image, new Offset2D(rect.X, rect.Y));

                                var sdf = rect.Data.First;
                                var pt1 = new Vector2(rect.X, rect.Y);
                                var pt2 = pt1 + new Vector2((float)sdf.Width, (float)sdf.Height);
                                var glyph = new LiveGlyphInfo
                                {
                                    AtlasHandle = ResourceHandle.InvalidTexture,
                                    State = LiveGlyphState.Pending,
                                    Size = new Vector2((float)sdf.Width, (float)sdf.Height),
                                    Coordinate = new Vector4(pt1 / AtlasSize, pt2.X / AtlasSize, pt2.Y / AtlasSize)
                                };
                                var key = toGenerate[rect.Data.Second].Second;
                                glyphsInAtlas.Add(key);
                                _atlases.AddOrUpdate(key, glyph, (_, _) => glyph);
                                sdf.Image.Dispose();
                            }
                        });

                        packedAtlas.CreateTexture(out _, graphicsModule: _graphicsModule).Then(handle =>
                        {
                            foreach (var key in glyphsInAtlas.Where(_atlases.ContainsKey))
                            {
                                var glyph = _atlases[key] with { State = LiveGlyphState.Ready, AtlasHandle = handle };
                                _atlases.AddOrUpdate(key, glyph, (_, _) => glyph);
                            }

                            packedAtlas.Dispose();
                        });
                    }
                }
                catch
                {
                    foreach (var (_, key) in toGenerate) _atlases.Remove(key, out _);
                    throw;
                }
            }, _cancellationSource.Token);
        }

        return Task.FromException(new Exception("Unknown font class"));
    }

    public unsafe void LoadFont(Stream fileStream)
    {
        using var memoryStream = new MemoryStream();
        fileStream.CopyTo(memoryStream);
        var bytes = memoryStream.ToArray();

        IntPtr face;
        fixed (byte* pBytes = bytes)
        {
            var blob = HarfBuzzNative.hb_blob_create(pBytes, (uint)bytes.Length, HarfBuzzNative.MemoryMode.Duplicate,
                IntPtr.Zero, IntPtr.Zero);
            face = HarfBuzzNative.hb_face_create(blob, 0);
            HarfBuzzNative.hb_blob_destroy(blob);
        }

        var font = HarfBuzzNative.hb_font_create(face);
        HarfBuzzNative.hb_ot_font_set_funcs(font);
        var unitsPerEm = HarfBuzzNative.hb_face_get_upem(face);
        HarfBuzzNative.hb_font_set_scale(font, (int)unitsPerEm, (int)unitsPerEm);

        var name = HarfBuzzNative.GetName(face, HarfBuzzNative.NameIdTypographicFamily)
                   ?? HarfBuzzNative.GetName(face, HarfBuzzNative.NameIdFontFamily)
                   ?? "Unknown";

        var insert = new HarfBuzzFont(face, font, name, unitsPerEm, this);
        _fonts.AddOrUpdate(name, insert, (_, existing) =>
        {
            existing.Dispose();
            return insert;
        });
    }

    public LiveGlyphInfo GetGlyph(IFont font, char character)
    {
        var key = new CacheKey(character, font.Name);
        return _atlases.GetValueOrDefault(key, _defaultLiveGlyph);
    }

    public IFont? GetFont(string name)
    {
        return _fonts.GetValueOrDefault(name);
    }

    public GlyphRect[] MeasureText(IFont font, in ReadOnlySpan<char> text, float size,
        float maxWidth = float.PositiveInfinity)
    {
        Debug.Assert(font is HarfBuzzFont);
        var myFont = (HarfBuzzFont)font;
        return GetCharacterBounds(myFont, text, size, maxWidth);
    }

    public void Dispose()
    {
        _cancellationSource.Cancel();
        _backgroundTaskQueue.Dispose();
        _graphicsModule
            .FreeResourceHandles(_atlases.Select(c => c.Value.AtlasHandle).Where(c => c.Id >= 0).ToArray());
        _atlases.Clear();
        foreach (var font in _fonts.Values) font.Dispose();
        _fonts.Clear();
    }

    public float GetPixelRange()
    {
        return PixelRange;
    }

    public IEnumerable<IFont> GetFonts()
    {
        return _fonts.Values;
    }

    /// <summary>
    ///     Shapes <paramref name="text" /> as a single run (ligatures disabled so cluster == char index, kerning left
    ///     on) and returns each character's horizontal advance in font units.
    /// </summary>
    private static unsafe float[] ShapeAdvances(HarfBuzzFont font, ReadOnlySpan<char> text)
    {
        var advances = new float[text.Length];
        if (text.Length == 0) return advances;

        var buffer = HarfBuzzNative.hb_buffer_create();
        try
        {
            fixed (char* pText = text)
            {
                HarfBuzzNative.hb_buffer_add_utf16(buffer, pText, text.Length, 0, text.Length);
            }

            HarfBuzzNative.hb_buffer_guess_segment_properties(buffer);

            var features = new[]
            {
                HarfBuzzNative.Feature.Toggle("liga", false),
                HarfBuzzNative.Feature.Toggle("clig", false),
                HarfBuzzNative.Feature.Toggle("dlig", false),
                HarfBuzzNative.Feature.Toggle("kern", true)
            };

            fixed (HarfBuzzNative.Feature* pFeatures = features)
            {
                HarfBuzzNative.hb_shape(font.Font, buffer, pFeatures, (uint)features.Length);
            }

            var infos = HarfBuzzNative.hb_buffer_get_glyph_infos(buffer, out var glyphCount);
            var positions = HarfBuzzNative.hb_buffer_get_glyph_positions(buffer, out _);

            for (var i = 0; i < glyphCount; i++)
            {
                var cluster = infos[i].Cluster;
                if (cluster < (uint)advances.Length) advances[cluster] += positions[i].XAdvance;
            }

            return advances;
        }
        finally
        {
            HarfBuzzNative.hb_buffer_destroy(buffer);
        }
    }

    /// <summary>
    ///     For each character, returns its own glyph ink box (offset from the pen position / line ascent, and ink
    ///     width/height) in pixels, derived from that specific glyph's own extents so the resulting box shares its
    ///     aspect ratio with the glyph's own rasterized SDF - <see cref="Content.TextBoxView.ComputeLayout" /> scales
    ///     a glyph's quad by dividing this box by the SDF's raster size (which is exactly the glyph's own ink
    ///     extents at <c>RenderSize</c>), so using anything else here (e.g. advance width or a shared line height)
    ///     mismatches the aspect ratio and stretches every glyph non-uniformly.
    /// </summary>
    private static void GetGlyphInkMetrics(HarfBuzzFont font, in ReadOnlySpan<char> text, float scale,
        float ascentPx, Span<float> xOffsets, Span<float> inkWidths, Span<float> topOffsets, Span<float> inkHeights)
    {
        for (var i = 0; i < text.Length; i++)
        {
            if (HarfBuzzNative.TryGetNominalGlyph(font.Font, text[i], out var glyphId) &&
                HarfBuzzNative.TryGetGlyphExtents(font.Font, glyphId, out var extents))
            {
                xOffsets[i] = extents.XBearing * scale;
                inkWidths[i] = extents.Width * scale;
                topOffsets[i] = ascentPx - extents.YBearing * scale;
                inkHeights[i] = -extents.Height * scale; // HarfBuzz reports height as negative (extends downward)
            }
            else
            {
                xOffsets[i] = 0f;
                inkWidths[i] = 0f;
                topOffsets[i] = 0f;
                inkHeights[i] = 0f;
            }
        }
    }

    /// <summary>
    ///     Greedy word-wrap: breaks at the last space that fits, falling back to a hard break mid-word when a single
    ///     word is wider than <paramref name="maxWidth" />.
    /// </summary>
    private static GlyphRect[] GetCharacterBounds(HarfBuzzFont font, in ReadOnlySpan<char> text, float size,
        float maxWidth = float.PositiveInfinity)
    {
        if (text.Length == 0) return [];

        var advancesInFontUnits = ShapeAdvances(font, text);
        var scale = font.UnitsPerEm == 0 ? 0f : size / font.UnitsPerEm;
        var lineHeight = font.GetLineHeight(size);
        var ascentPx = HarfBuzzNative.TryGetHorizontalFontExtents(font.Font, out var fontExtents)
            ? fontExtents.Ascender * scale
            : lineHeight;

        var xOffsets = new float[text.Length];
        var inkWidths = new float[text.Length];
        var topOffsets = new float[text.Length];
        var inkHeights = new float[text.Length];
        GetGlyphInkMetrics(font, text, scale, ascentPx, xOffsets, inkWidths, topOffsets, inkHeights);

        var results = new GlyphRect[text.Length];
        var penX = 0f;
        var penY = 0f;
        var lineStart = 0;
        var lastSpace = -1;

        for (var i = 0; i < text.Length; i++)
        {
            var ch = text[i];
            var advance = advancesInFontUnits[i] * scale;

            if (ch == '\n')
            {
                results[i] = new GlyphRect
                {
                    Character = ch, Position = new Vector2(penX + xOffsets[i], penY + topOffsets[i]),
                    Size = new Vector2(inkWidths[i], inkHeights[i]),
                    PenX = penX, Advance = advance, LineTop = penY, LineHeight = lineHeight
                };
                penX = 0f;
                penY += lineHeight;
                lineStart = i + 1;
                lastSpace = -1;
                continue;
            }

            if (float.IsFinite(maxWidth) && i > lineStart && penX + advance > maxWidth)
            {
                var newLineStart = lastSpace >= lineStart ? lastSpace + 1 : i;

                if (newLineStart > lineStart)
                {
                    var carried = 0f;
                    for (var k = newLineStart; k < i; k++)
                    {
                        results[k].Position = new Vector2(carried + xOffsets[k], penY + lineHeight + topOffsets[k]);
                        results[k].PenX = carried;
                        results[k].LineTop = penY + lineHeight;
                        carried += advancesInFontUnits[k] * scale;
                    }

                    penX = carried;
                }
                else
                {
                    penX = 0f;
                }

                penY += lineHeight;
                lineStart = newLineStart;
                lastSpace = -1;
            }

            if (ch == ' ') lastSpace = i;

            results[i] = new GlyphRect
            {
                Character = ch, Position = new Vector2(penX + xOffsets[i], penY + topOffsets[i]),
                Size = new Vector2(inkWidths[i], inkHeights[i]),
                PenX = penX, Advance = advance, LineTop = penY, LineHeight = lineHeight
            };
            penX += advance;
        }

        return results;
    }

    private SdfResult? GenerateGlyph(char character, HarfBuzzFont font, CacheKey cacheKey)
    {
        var sdfCacheKey = $"Font/{cacheKey.FontName}/{cacheKey.Character}";
        
        if (_cache is not null && _cache.HasVector(sdfCacheKey))
        {
            var vector = _cache.GetVector(sdfCacheKey)!;
            var image = _cache.LoadImage(vector.ImageId)!;
            return new SdfResult(image, vector.Size.X, vector.Size.Y);
        }

        {
            HarfBuzzNative.TryGetNominalGlyph(font.Font, character, out var glyphId);

            var scale = font.UnitsPerEm == 0 ? 0f : RenderSize / font.UnitsPerEm;
            using var renderer = new MtsdfTextRenderer(scale, font.UsesPostScriptOutlines);
            HarfBuzzNative.DrawGlyph(font.Font, glyphId, renderer);
            var result = renderer.Generate(3f, GetPixelRange());

            if (result is null || _cache is null || _cache.HasVector(sdfCacheKey)) return result;

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

            return result;
        }
    }

    private record struct CacheKey(char Character, string FontName)
    {
        public readonly char Character = Character;
        public readonly string FontName = FontName;
    }
}

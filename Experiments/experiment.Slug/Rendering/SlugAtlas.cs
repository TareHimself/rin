using System.Numerics;
using System.Runtime.InteropServices;
using Rin.Core.Graphics;
using Rin.Core.Shared.Buffers;
using SixLabors.Fonts;

namespace experiment.Slug.Rendering;

// Per-shape metadata needed to build a draw instance once the shape lives in the atlas.
internal sealed class AtlasShapeEntry
{
    public int BandTexX;
    public int BandTexY;
    public int BandMaxX;
    public int BandMaxY;
    public Vector2 BoundsMin;
    public Vector2 BoundsMax;

    // em-coordinate -> band-index transform: bandIndex = emCoord * BandScale + BandOffset.
    public Vector2 BandScale;
    public Vector2 BandOffset;
}

// Owns the two GPU-resident textures the SLUG shader reads as flat data arrays:
//   - curve texture (RGBA32): 2 texels per quadratic Bezier curve
//   - band texture  (RG32):   per-shape band headers (count, offset) + curve index lists
// Both grow as shapes are added; call EnsureUploaded() once per frame before drawing (it's a
// no-op unless new shapes were added since the last upload).
public sealed class SlugAtlas : IDisposable
{
    // Row width in texels for both textures — must match kBandWidth in slug.slang.
    private const int TextureWidth = 4096;

    private readonly List<float> _curveData = [];
    private readonly List<float> _bandData = [];
    private int _curveCursor;
    private int _bandCursor;

    private readonly List<AtlasShapeEntry> _entries = [];
    private readonly Dictionary<(string Family, float Size, char Codepoint), uint> _glyphCache = [];

    private bool _dirty;

    public ResourceHandle CurveHandle { get; private set; }
    public ResourceHandle BandHandle { get; private set; }

    // Register an arbitrary closed vector shape; returns a stable id for draw calls.
    public uint AddPath(VectorPath path, int bandsX = 8, int bandsY = 8)
    {
        var packed = ShapeBandPacker.Pack(path, bandsX, bandsY);

        var curveBase = _curveCursor;
        _curveData.AddRange(packed.CurveTexels);
        _curveCursor += path.Curves.Count * 2;

        var bandBase = _bandCursor;
        var totalHeaders = packed.BandCountY + packed.BandCountX;
        var headerStart = _bandData.Count;

        for (var i = 0; i < totalHeaders; i++) _bandData.AddRange([0f, 0f]);
        _bandCursor += totalHeaders;

        var hOffsets = new int[packed.BandCountY];
        for (var b = 0; b < packed.BandCountY; b++)
        {
            hOffsets[b] = _bandCursor - bandBase;
            foreach (var curveIndex in packed.HBandCurves[b])
                WriteCurveRef(curveBase, curveIndex);
        }

        var vOffsets = new int[packed.BandCountX];
        for (var b = 0; b < packed.BandCountX; b++)
        {
            vOffsets[b] = _bandCursor - bandBase;
            foreach (var curveIndex in packed.VBandCurves[b])
                WriteCurveRef(curveBase, curveIndex);
        }

        for (var b = 0; b < packed.BandCountY; b++)
        {
            var idx = headerStart + b * 2;
            _bandData[idx + 0] = packed.HBandCurves[b].Count;
            _bandData[idx + 1] = hOffsets[b];
        }
        for (var b = 0; b < packed.BandCountX; b++)
        {
            var idx = headerStart + (packed.BandCountY + b) * 2;
            _bandData[idx + 0] = packed.VBandCurves[b].Count;
            _bandData[idx + 1] = vOffsets[b];
        }

        var size = Vector2.Max(packed.BoundsMax - packed.BoundsMin, new Vector2(1e-4f));
        var bandCount = new Vector2(packed.BandCountX, packed.BandCountY);
        var bandScale = bandCount / size;
        var bandOffset = -packed.BoundsMin * bandScale;

        _entries.Add(new AtlasShapeEntry
        {
            BandTexX = bandBase % TextureWidth,
            BandTexY = bandBase / TextureWidth,
            BandMaxX = packed.BandCountX - 1,
            BandMaxY = packed.BandCountY - 1,
            BoundsMin = packed.BoundsMin,
            BoundsMax = packed.BoundsMax,
            BandScale = bandScale,
            BandOffset = bandOffset
        });

        _dirty = true;
        return (uint)(_entries.Count - 1);

        void WriteCurveRef(int curveBaseTexel, int localCurveIndex)
        {
            var absolute = curveBaseTexel + localCurveIndex * 2;
            _bandData.Add(absolute % TextureWidth);
            _bandData.Add(absolute / TextureWidth);
            _bandCursor++;
        }
    }

    // Add (or reuse) the outline for one glyph, cached by (font family, size, codepoint).
    public uint GetOrAddGlyph(Font font, char ch)
    {
        var key = (font.Family.Name, font.Size, ch);
        if (_glyphCache.TryGetValue(key, out var cached)) return cached;

        var extractor = new GlyphOutlineExtractor();
        TextRenderer.RenderTextTo(extractor, ch.ToString(), new TextOptions(font));

        var id = AddPath(extractor.GetNormalizedPath());
        _glyphCache[key] = id;
        return id;
    }

    internal AtlasShapeEntry GetEntry(uint shapeId) => _entries[(int)shapeId];

    public void EnsureUploaded()
    {
        if (!_dirty && CurveHandle.IsValid()) return;

        UploadTexture(_curveData, _curveCursor, 4, ImageFormat.RGBA32, out var curveHandle);
        UploadTexture(_bandData, _bandCursor, 2, ImageFormat.RG32, out var bandHandle);

        CurveHandle = curveHandle;
        BandHandle = bandHandle;
        _dirty = false;
    }

    public void Dispose()
    {
        if (CurveHandle.IsValid()) IGraphicsModule.Get().FreeResourceHandles(CurveHandle);
        if (BandHandle.IsValid()) IGraphicsModule.Get().FreeResourceHandles(BandHandle);
    }

    private static void UploadTexture(List<float> data, int usedTexels, int channels, ImageFormat format,
        out ResourceHandle handle)
    {
        var height = Math.Max(1, (int)Math.Ceiling(usedTexels / (double)TextureWidth));
        var extent = new Extent2D((uint)TextureWidth, (uint)height);

        var needed = TextureWidth * height * channels;
        while (data.Count < needed) data.Add(0f);

        var floatSpan = CollectionsMarshal.AsSpan(data)[..needed];
        var byteSpan = MemoryMarshal.AsBytes(floatSpan);

        using var buffer = new Buffer<byte>(byteSpan);
        IGraphicsModule.Get()
            .CreateTexture(out handle, buffer, extent, format)
            .GetAwaiter().GetResult();
    }
}

using System.Numerics;
using System.Runtime.InteropServices;
using Rin.Core.Graphics;
using Rin.Core.Shared.Buffers;
using SixLabors.Fonts;

namespace misc.VectorRendering.Slug;

// Records the location and band parameters for one shape in the atlas textures.
internal sealed class ShapeAtlasEntry
{
    // Absolute texel coordinate of this shape's band data block origin (glyphLoc).
    public int BandTexX;
    public int BandTexY;

    // Maximum band indices for the shader's clamp.
    public int BandMaxX;
    public int BandMaxY;

    // Bounding box in glyph-relative (em) coordinates.
    public Vector2 BoundsMin;
    public Vector2 BoundsMax;

    // Transform from em-coordinate to band index:
    //   bandIndex.x = emCoord.x * BandScaleX + BandOffsetX  (for vertical bands)
    //   bandIndex.y = emCoord.y * BandScaleY + BandOffsetY  (for horizontal bands)
    public float BandScaleX, BandOffsetX;
    public float BandScaleY, BandOffsetY;
}

// Manages the two persistent GPU textures used by the SLUG algorithm:
//   - Curve texture (RGBA32F): packed bezier control points, 2 texels per curve
//   - Band texture  (RGBA32F): band acceleration structure (headers + curve index lists)
//
// Both textures grow as shapes are added and are re-uploaded on EnsureUploaded().
// Shapes are indexed by a stable uint ID returned from AddShape().
//
// Usage pattern:
//   var atlas = new SlugAtlas();
//   var shapeId = atlas.AddShape(VectorPath.FromCurves(curves));
//   atlas.EnsureUploaded();   // uploads once before the render loop
//   // then use atlas.CurveHandle / atlas.BandHandle in SlugCommand draws
public class SlugAtlas : IDisposable
{
    // Fixed texture width matching the shader constant kBandWidth = 4096.
    // Both curve and band textures use this width; rows wrap automatically.
    private const int TextureWidth = 4096;

    // Raw float data accumulating as shapes are added.
    // 4 floats per texel, width = TextureWidth.
    private readonly List<float> _curveData = [];
    private readonly List<float> _bandData  = [];

    // Cursor positions in texel units (not float units).
    private int _curveCursor = 0;
    private int _bandCursor  = 0;

    // Per-shape metadata: index = shape ID.
    private readonly List<ShapeAtlasEntry> _entries = [];

    // Font glyph cache: (familyName+size, codepoint) → shape ID.
    private readonly Dictionary<(string, float, char), uint> _glyphCache = [];

    // Set to true when shapes have been added since the last upload.
    private bool _dirty = false;

    // Current GPU texture handles — valid after EnsureUploaded().
    public ResourceHandle CurveHandle { get; private set; }
    public ResourceHandle BandHandle  { get; private set; }

    // Add an arbitrary vector shape to the atlas.
    // Returns a stable uint ID that can be passed to GlyphDrawData for rendering.
    public uint AddShape(VectorPath path)
    {
        var packed = SlugShapePacker.Pack(path);

        // ---- Stitch curve texture ----
        // The curve texture is a simple linear array of curve records.
        // Each curve = 2 texels = 8 floats; we record the base texel index
        // so band entries can reference curves by absolute (x, y) coordinates.
        int curveBase = _curveCursor; // absolute texel index of this shape's first curve texel
        _curveData.AddRange(packed.CurveTexels);
        _curveCursor += path.Curves.Count * 2; // each curve = 2 texels

        // ---- Stitch band texture ----
        // Layout at this shape's block (starting at _bandCursor):
        //   [H band 0 header] [H band 1 header] ... [H band (bandsY-1) header]
        //   [V band 0 header] [V band 1 header] ... [V band (bandsX-1) header]
        //   [H band 0 index list] [H band 1 index list] ...
        //   [V band 0 index list] [V band 1 index list] ...
        //
        // Headers: (count, offset_from_shape_start, 0, 0) stored as floats.
        // Index entries: (curveTexelX, curveTexelY, 0, 0) — absolute curve texture coords.
        //
        // offset_from_shape_start is measured in texels from the beginning of this shape's
        // block.  The shader uses CalcBandLoc(glyphLoc, offset) which handles row wrapping.
        int bandBase = _bandCursor;

        int totalHeaders    = packed.BandCountY + packed.BandCountX;
        int headerFlatStart = _bandData.Count; // _bandData index of first header float

        // Reserve header space (4 floats each, filled in after we know offsets).
        for (var i = 0; i < totalHeaders; i++)
            _bandData.AddRange([0f, 0f, 0f, 0f]);
        _bandCursor += totalHeaders;

        // Write H band index lists, recording the offset for each header.
        var hOffsets = new int[packed.BandCountY];
        for (var b = 0; b < packed.BandCountY; b++)
        {
            hOffsets[b] = _bandCursor - bandBase; // offset in texels from this shape's origin

            foreach (var ci in packed.HBandCurves[b])
            {
                // Translate local curve index → absolute curve-texture (x, y) coordinate.
                int absoluteTexel = curveBase + ci * 2;
                int cx = absoluteTexel % TextureWidth;
                int cy = absoluteTexel / TextureWidth;
                _bandData.AddRange([(float)cx, (float)cy, 0f, 0f]);
                _bandCursor++;
            }
        }

        // Write V band index lists.
        var vOffsets = new int[packed.BandCountX];
        for (var b = 0; b < packed.BandCountX; b++)
        {
            vOffsets[b] = _bandCursor - bandBase;

            foreach (var ci in packed.VBandCurves[b])
            {
                int absoluteTexel = curveBase + ci * 2;
                int cx = absoluteTexel % TextureWidth;
                int cy = absoluteTexel / TextureWidth;
                _bandData.AddRange([(float)cx, (float)cy, 0f, 0f]);
                _bandCursor++;
            }
        }

        // Backfill header slots now that we know all offsets and counts.
        for (var b = 0; b < packed.BandCountY; b++)
        {
            int idx = headerFlatStart + b * 4;
            _bandData[idx + 0] = packed.HBandCurves[b].Count;
            _bandData[idx + 1] = hOffsets[b];
        }
        for (var b = 0; b < packed.BandCountX; b++)
        {
            int idx = headerFlatStart + (packed.BandCountY + b) * 4;
            _bandData[idx + 0] = packed.VBandCurves[b].Count;
            _bandData[idx + 1] = vOffsets[b];
        }

        // Compute the banding transform for the shader.
        // bandIndex.x = emCoord.x * scaleX + offsetX  (which vertical band?)
        // bandIndex.y = emCoord.y * scaleY + offsetY  (which horizontal band?)
        var size = packed.BoundsMax - packed.BoundsMin;
        float scaleX  = packed.BandCountX / MathF.Max(size.X, 1e-4f);
        float scaleY  = packed.BandCountY / MathF.Max(size.Y, 1e-4f);
        float offsetX = -packed.BoundsMin.X * scaleX;
        float offsetY = -packed.BoundsMin.Y * scaleY;

        var entry = new ShapeAtlasEntry
        {
            BandTexX   = bandBase % TextureWidth,
            BandTexY   = bandBase / TextureWidth,
            BandMaxX   = packed.BandCountX - 1,
            BandMaxY   = packed.BandCountY - 1,
            BoundsMin  = packed.BoundsMin,
            BoundsMax  = packed.BoundsMax,
            BandScaleX = scaleX,
            BandScaleY = scaleY,
            BandOffsetX = offsetX,
            BandOffsetY = offsetY
        };

        _entries.Add(entry);
        _dirty = true;
        return (uint)(_entries.Count - 1);
    }

    // Convenience: add (or return cached) the bezier outline for a single glyph.
    // The curves are extracted using SixLabors' IGlyphRenderer infrastructure and
    // normalized to the glyph's own coordinate system (origin at bounds top-left).
    public uint GetOrAddGlyph(Font font, char ch)
    {
        var key = (font.Family.Name + font.Size.ToString("F1"), font.Size, ch);
        if (_glyphCache.TryGetValue(key, out var cached))
            return cached;

        var renderer = new SlugGlyphRenderer();
        TextRenderer.RenderTextTo(renderer, ch.ToString(), new TextOptions(font));

        var paths = renderer.GetNormalizedPaths();

        // Merge all contours of this glyph into one VectorPath.
        // (A glyph like 'B' or 'O' has multiple closed contours; we pack them together.)
        var allCurves = paths.SelectMany(p => p.Curves).ToArray();
        var id = allCurves.Length > 0
            ? AddShape(VectorPath.FromCurves(allCurves))
            : AddShape(new VectorPath([], Vector2.Zero, Vector2.Zero));

        _glyphCache[key] = id;
        return id;
    }

    // Returns the atlas entry for a previously added shape (for building draw data).
    internal ShapeAtlasEntry GetEntry(uint shapeId) => _entries[(int)shapeId];

    // Upload (or re-upload) the curve and band textures to the GPU.
    // Call this once after all AddShape() calls, before submitting draw commands.
    // Blocks until the transfer is complete.
    public void EnsureUploaded()
    {
        if (!_dirty && CurveHandle.IsValid()) return;

        UploadTexture(_curveData, _curveCursor, out var curveHandle);
        UploadTexture(_bandData,  _bandCursor,  out var bandHandle);

        CurveHandle = curveHandle;
        BandHandle  = bandHandle;
        _dirty      = false;
    }

    public void Dispose()
    {
        if (CurveHandle.IsValid()) IGraphicsModule.Get().FreeResourceHandles(CurveHandle);
        if (BandHandle.IsValid())  IGraphicsModule.Get().FreeResourceHandles(BandHandle);
    }

    // Convert a List<float> into a GPU RGBA32F texture.
    // The texture is always TextureWidth wide; rows wrap automatically.
    private static void UploadTexture(List<float> data, int usedTexels, out ResourceHandle handle)
    {
        // Compute minimum height needed to hold all used texels.
        int height = Math.Max(1, (int)Math.Ceiling(usedTexels / (double)TextureWidth));
        var extent = new Extent2D((uint)TextureWidth, (uint)height);

        // Ensure the float list is large enough for the full (width × height) extent
        // so the GPU upload has a complete rectangular region to copy.
        int needed = TextureWidth * height * 4; // 4 floats per texel (RGBA)
        while (data.Count < needed) data.Add(0f);

        // Reinterpret the float data as raw bytes for the transfer API.
        // MemoryMarshal.AsBytes is a zero-copy reinterpretation — no allocation.
        var floatSpan = CollectionsMarshal.AsSpan(data)[..needed];
        var byteSpan  = MemoryMarshal.AsBytes(floatSpan);

        using var buffer = new Buffer<byte>(byteSpan);
        IGraphicsModule.Get()
            .CreateTexture(out handle, buffer, extent, ImageFormat.RGBA32)
            .GetAwaiter().GetResult();
    }
}

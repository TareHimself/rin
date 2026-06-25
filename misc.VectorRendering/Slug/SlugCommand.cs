using System.Numerics;
using SixLabors.Fonts;
using Rin.Core.Views.Graphics;
using Rin.Core.Views.Graphics.Commands;
using Rin.Core.Views.Graphics.PassConfigs;

namespace misc.VectorRendering.Slug;

// Batches one or more vector shape draw calls into a single command.
// Each SlugCommand belongs to a single SlugAtlas (sharing the same curve + band textures).
// All shapes in the command are drawn as instanced quads in one GPU draw call.
public class SlugCommand : TCommand<MainPassConfig, SlugHandler>
{
    // The atlas providing the curve and band textures for these draws.
    // Atlas.EnsureUploaded() must be called before Execute() runs.
    public required SlugAtlas Atlas;

    // One entry per shape instance — fed directly into the GPU instance buffer.
    public required List<GlyphDrawData> Draws;
}

// Extension methods that let views add SLUG draws to a CommandList naturally.
// These mirror the pattern of QuadExtensions.AddRect / AddQuads etc.
public static class SlugCommandExtensions
{
    // Add a single previously-registered vector shape at the given screen position.
    // `shapeId` must have been returned by atlas.AddShape() or atlas.GetOrAddGlyph().
    // `position` is the top-left corner of the shape on screen.
    // `scale` uniformly scales the shape's bounding box.
    public static CommandList AddVectorShape(
        this CommandList list,
        SlugAtlas        atlas,
        uint             shapeId,
        Vector2          position,
        float            scale,
        Vector4          color)
    {
        var entry = atlas.GetEntry(shapeId);

        // Expand the screen quad by 1 pixel on each side to include partially-covered
        // boundary pixels (the SLUG AA kernel extends ±0.5 pixels past the exact edge).
        var expand  = new Vector2(1f);
        var minPos  = position + entry.BoundsMin * scale - expand;
        var maxPos  = position + entry.BoundsMax * scale + expand;
        var minEm   = entry.BoundsMin - expand / scale;
        var maxEm   = entry.BoundsMax + expand / scale;

        var draw = new GlyphDrawData
        {
            MinPos    = minPos,
            MaxPos    = maxPos,
            MinEm     = minEm,
            MaxEm     = maxEm,
            Banding   = new Vector4(entry.BandScaleX, entry.BandScaleY,
                                    entry.BandOffsetX, entry.BandOffsetY),
            ShapeLocX = entry.BandTexX,
            ShapeLocY = entry.BandTexY,
            BandMaxX  = entry.BandMaxX,
            BandMaxY  = entry.BandMaxY,
            Color     = color
        };

        // Look for an existing SlugCommand with the same atlas to batch into.
        if (FindExistingCommand(list, atlas) is { } existing)
        {
            existing.Draws.Add(draw);
        }
        else
        {
            list.Add(new SlugCommand
            {
                Atlas = atlas,
                Draws = [draw]
            });
        }

        return list;
    }

    // Lay out a string of text and add a SLUG draw for each glyph.
    // Glyphs are cached in the atlas by character identity, so repeating the same
    // character (e.g. "Hello") only packs its outline once.
    public static CommandList AddVectorText(
        this CommandList list,
        SlugAtlas        atlas,
        string           text,
        Font             font,
        Vector2          position,
        Vector4          color,
        float            scale = 1f)
    {
        // Use SixLabors to measure per-character layout bounds so we can position
        // each glyph's quad accurately on screen.
        if (!TextMeasurer.TryMeasureCharacterBounds(text, new TextOptions(font), out var bounds))
            return list;

        for (var i = 0; i < text.Length && i < bounds.Length; i++)
        {
            var ch    = text[i];
            var bound = bounds[i];

            if (char.IsWhiteSpace(ch)) continue;

            var shapeId = atlas.GetOrAddGlyph(font, ch);

            // The character bound is in the SixLabors layout space (from text origin).
            // We add the caller's `position` to get screen coordinates.
            var charOffset = new Vector2(bound.Bounds.X, bound.Bounds.Y);
            AddVectorShape(list, atlas, shapeId, position + charOffset * scale, scale, color);
        }

        return list;
    }

    // Scan the CommandList for an existing SlugCommand using the given atlas,
    // so we can batch additional draws without creating a new GPU draw call.
    private static SlugCommand? FindExistingCommand(CommandList list, SlugAtlas atlas)
    {
        for (var i = list.Commands.Count - 1; i >= 0; i--)
            if (list.Commands[i] is SlugCommand sc && sc.Atlas == atlas)
                return sc;
        return null;
    }
}

using System.Numerics;
using JetBrains.Annotations;
using Rin.Core.Graphics;
using Rin.Core.Views.Font;
using Rin.Core.Views.Graphics;
using Rin.Core.Views.Graphics.Quads;

namespace Rin.Core.Views.Content;

/// <summary>
///     Draws a single glyph from a font as a colorable icon, using the same MTSDF pipeline as
///     <see cref="TextBoxView" /> - suitable for icon fonts (e.g. Font Awesome) where each icon is a Private Use
///     Area codepoint in an otherwise ordinary OTF/TTF loaded via <see cref="IFontManager.LoadFont" />.
/// </summary>
public class FontIconView : ContentView
{
    private GlyphRect? _cachedBound;
    private string _fontFamily = "Noto Sans";
    private IFontManager _fontManager;
    private char _icon;
    private float _iconSize = 32.0f;

    public FontIconView(IViewsModule? viewsModule = null)
    {
        _fontManager = (viewsModule ?? IViewsModule.Get()).FontManager;
        MakeNewFont();
    }

    [PublicAPI] protected IFont? CurrentFont { get; private set; }

    [PublicAPI] public Color Color { get; set; } = Color.White;

    [PublicAPI]
    public IFontManager FontManager
    {
        get => _fontManager;
        set
        {
            _fontManager = value;
            MakeNewFont();
        }
    }

    [PublicAPI]
    public string FontFamily
    {
        get => _fontFamily;
        set
        {
            _fontFamily = value;
            MakeNewFont();
        }
    }

    [PublicAPI]
    public float IconSize
    {
        get => _iconSize;
        set
        {
            _iconSize = value;
            _cachedBound = null;
            InvalidateDesiredSize();
            InvalidateLayout();
        }
    }

    [PublicAPI]
    public char Icon
    {
        get => _icon;
        set
        {
            if (_icon == value) return;
            _icon = value;
            _cachedBound = null;
            InvalidateDesiredSize();
            InvalidateLayout();
        }
    }

    protected bool FontReady => CurrentFont != null;

    private void MakeNewFont()
    {
        if (_fontManager.GetFont(FontFamily) is { } font) CurrentFont = font;
        _cachedBound = null;
        InvalidateDesiredSize();
        InvalidateLayout();
    }

    private GlyphRect? GetBound()
    {
        if (CurrentFont == null || _icon == '\0') return null;
        if (_cachedBound is { } cached) return cached;

        var bounds = _fontManager.MeasureText(CurrentFont, [_icon], _iconSize);
        return bounds.Length == 0 ? null : _cachedBound = bounds[0];
    }

    protected override Vector2 LayoutContent(in Vector2 availableSpace)
    {
        return ComputeDesiredContentSize();
    }

    public override Vector2 ComputeDesiredContentSize()
    {
        return GetBound() is { } bound ? new Vector2(bound.PenX + bound.Advance, bound.Bottom) : Vector2.Zero;
    }

    public override void CollectContent(in Matrix4x4 transform, CommandList commands)
    {
        if (CurrentFont == null || GetBound() is not { } bound) return;

        var range = _fontManager.GetPixelRange();
        var glyph = _fontManager.GetGlyph(CurrentFont, _icon);

        if (glyph.State == LiveGlyphState.Invalid)
        {
            _fontManager.Prepare(CurrentFont, [_icon]);
            return;
        }

        if (glyph.State != LiveGlyphState.Ready) return;

        var (glyphTransform, size) = MtsdfGlyphLayout.Compute(bound, glyph, range);
        commands.AddMtsdf(glyph.AtlasHandle, glyphTransform * transform, size, range, Color, glyph.Coordinate);
    }
}

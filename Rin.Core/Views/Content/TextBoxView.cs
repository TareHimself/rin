using System.Numerics;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using Rin.Core.Graphics;
using Rin.Core.Views.Font;
using Rin.Core.Views.Graphics;
using Rin.Core.Views.Graphics.Quads;
using Rin.Core.Extensions;
using Rin.Core.Shared.Math;

namespace Rin.Core.Views.Content;

/// <summary>
///     Draw's text using a loaded font family.
/// </summary>
public class TextBoxView : ContentView
{
    private GlyphRect[]? _cachedBounds;
    private CachedQuadLayout[]? _cachedLayouts;

    private string _content = string.Empty;
    private string _fontFamily = "Noto Sans";
    private IFontManager _fontManager;
    private float _fontSize = 100.0f;
    private bool _wrapContent;
    protected float? Wrap;

    public TextBoxView(IViewsModule? viewsModule = null)
    {
        _fontManager = (viewsModule ?? IViewsModule.Get()).FontManager;
        _cachedLayouts = null;
        _cachedBounds = null;
        MakeNewFont();
    }

    [PublicAPI] protected IFont? CurrentFont { get; private set; }

    protected float LineHeight => CurrentFont?.GetLineHeight(FontSize) ?? 0;

    [PublicAPI] public Color ForegroundColor { get; set; } = Color.White;

    [PublicAPI] public Color BackgroundColor { get; set; } = Color.White with { A = 0.0f };

    [PublicAPI]
    public bool WrapContent
    {
        get => _wrapContent;
        set
        {
            if (value != _wrapContent)
            {
                _cachedLayouts = null;
                _cachedBounds = null;
            }
            _wrapContent = value;
            InvalidateLayout();
        }
    }

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
    public float FontSize
    {
        get => _fontSize;
        set
        {
            _cachedLayouts = null;
            _fontSize = value;
            MakeNewFont();
        }
    }

    [PublicAPI]
    public string Content
    {
        get => _content;
        set
        {
            var hasChanged = _content != value;
            if (hasChanged) TextChanged(value);
        }
    }

    protected bool FontReady => CurrentFont != null;

    protected virtual void TextChanged(string newText)
    {
        _cachedLayouts = ComputeLayout(out var pending);
        if (pending) _cachedLayouts = null;
        _cachedBounds = null;
        _content = newText;
        // TextRenderer.RenderTextTo();
        InvalidateDesiredSize();
        InvalidateLayout();
    }

    protected override Vector2 LayoutContent(in Vector2 availableSpace)
    {
        _cachedLayouts = null;
        Wrap = _wrapContent ? float.IsFinite(availableSpace.X) ? availableSpace.X + 2f : null : null;
        var bounds = GetCharacterBounds(Wrap);
        if (bounds.Empty()) return new Vector2(0.0f, LineHeight);
        // PenX + Advance (not the ink-based Right) so trailing whitespace still reserves its layout space.
        var width = bounds.Max(c => c.PenX + c.Advance);
        var height = bounds.MaxBy(c => c.Bottom).Bottom;
        return new Vector2(width, height);
    }

    private void MakeNewFont()
    {
        if (_fontManager.GetFont(FontFamily) is { } font) CurrentFont = font;
        _cachedBounds = null;
        _cachedLayouts = ComputeLayout(out var pending);
        if (pending) _cachedLayouts = null;
        InvalidateDesiredSize();
        InvalidateLayout();
    }


    protected GlyphRect[] GetCharacterBounds(float? wrap = null, bool cache = true)
    {
        if (CurrentFont == null) return [];

        if (cache && _cachedBounds is { } cached) return cached;

        var bounds = FontManager.MeasureText(CurrentFont, Content, FontSize, wrap ?? float.PositiveInfinity);

        if (cache) return _cachedBounds = bounds;

        return bounds;
    }

    public override Vector2 ComputeDesiredContentSize()
    {
        if (Content.Empty() || CurrentFont == null) return new Vector2(0.0f, LineHeight);
        var bounds = GetCharacterBounds(cache: false);
        var width = bounds.Empty() ? 0 : bounds.Max(c => c.PenX + c.Advance);
        var lines = float.Max(1, Content.Split("\n").Length);
        var height = LineHeight * lines;

        return new Vector2(width, bounds.Empty() ? 0 : bounds.MaxBy(c => c.Bottom).Bottom);
    }

    protected CachedQuadLayout[] ComputeLayout(out bool anyPending)
    {
        if (CurrentFont == null || Content.Empty())
        {
            anyPending = false;
            return [];
        }

        var pending = false;
        List<CachedQuadLayout> results = [];
        foreach (var bound in GetCharacterBounds(Wrap))
        {
            var range = _fontManager.GetPixelRange();
            var glyph = _fontManager.GetGlyph(CurrentFont, bound.Character);
            if (glyph.State == LiveGlyphState.Invalid && bound.Character.IsPrintable())
            {
                _fontManager.Prepare(CurrentFont, [bound.Character]);
                pending = true;
            }
            else if (glyph.State == LiveGlyphState.Pending)
            {
                pending = true;
            }

            if (glyph.State != LiveGlyphState.Ready) continue;

            var charOffset = bound.Position;

            var size = bound.Size;
            var vectorSize = glyph.Size - new Vector2(range * 2);
            var scale = size / vectorSize;
            var pxRangeScaled = new Vector2(range) * scale;
            size += pxRangeScaled * 2;

            charOffset -= pxRangeScaled;

            var finalTransform = Matrix4x4.Identity.Scale(new Vector2(1.0f, -1.0f)).Translate(charOffset with
            {
                Y = charOffset.Y + size.Y
            });

            results.Add(new CachedQuadLayout(glyph.AtlasHandle, finalTransform, size, glyph.Coordinate));
        }

        anyPending = pending;

        return results.ToArray();
    }
    
    private readonly List<Quad> _collectCacheQuads = [];
    public override void CollectContent(in Matrix4x4 transform, CommandList commands)
    {
        _collectCacheQuads.Clear();
        if (CurrentFont == null) return;
        if (Content.NotEmpty() && _cachedLayouts == null)
        {
            var layout = ComputeLayout(out var hadAnyPending);
            var x4 = transform;
            _collectCacheQuads.AddRange(layout.Select(c => Quad.Mtsdf(c.Atlas, c.Transform * x4, c.Size, Color.White,
                c.Uv)));
            if (_collectCacheQuads.Count == 0) return;
            if (!hadAnyPending) _cachedLayouts = layout;
        }
        else if (_cachedLayouts != null)
        {
            var x4 = transform;
            foreach (var quad in _cachedLayouts)
            {
                _collectCacheQuads.Add(Quad.Mtsdf(quad.Atlas, quad.Transform * x4, quad.Size, Color.White, quad.Uv));
            }
        }

        if (_collectCacheQuads.Count > 0)
        {
            commands.Add(new QuadDrawCommand(CollectionsMarshal.AsSpan(_collectCacheQuads)));
        }
    }

    protected struct CachedQuadLayout(in ResourceHandle atlas, Matrix4x4 transform, Vector2 size, Vector4 uv)
    {
        public readonly ResourceHandle Atlas = atlas;
        public Matrix4x4 Transform = transform;
        public Vector2 Size = size;
        public Vector4 Uv = uv;
    }
}
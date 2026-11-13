using System.Numerics;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using Rin.Core.Graphics;
using Rin.Core.Views.Font;
using Rin.Core.Views.Graphics;
using Rin.Core.Views.Graphics.Quads;
using Rin.Core.Extensions;
using Rin.Core.Shared;
using Rin.Core.Shared.Math;

namespace Rin.Core.Views.Content;

/// <summary>
///     Draw's text using a loaded font family.
/// </summary>
public class TextBoxView : ContentView
{
    private PooledMemory<GlyphRect>? _cachedBounds;
    private PooledMemory<CachedQuadLayout>? _cachedLayouts;

    private string _content = string.Empty;
    private string _fontFamily;
    private IFontManager _fontManager;
    private float _fontSize = 100.0f;
    private bool _wrapContent;
    protected float? Wrap;

    public TextBoxView(IViewsModule? viewsModule = null)
    {
        var module = viewsModule ?? IViewsModule.Get();
        _fontManager = module.FontManager;
        _fontFamily = module.DefaultFontFamily;
        _cachedLayouts = null;
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
                ReleaseLayouts();
                InvalidateCharacterBounds();
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
            ReleaseLayouts();
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
        _content = newText;
        ReleaseLayouts();
        InvalidateCharacterBounds();
        InvalidateDesiredSize();
        InvalidateLayout();
    }

    protected override Vector2 LayoutContent(in Vector2 availableSpace)
    {
        ReleaseLayouts();
        Wrap = _wrapContent ? float.IsFinite(availableSpace.X) ? availableSpace.X + 2f : null : null;
        var bounds = GetCharacterBounds();
        if (bounds.IsEmpty) return new Vector2(0.0f, LineHeight);
        var extent = MeasureExtent(bounds);
        // At least a full line, so text without descenders is as tall as text with them.
        return extent with { Y = float.Max(extent.Y, LineHeight) };
    }

    /// <summary>
    ///     The widest pen extent and the lowest ink edge of <paramref name="bounds" />.
    /// </summary>
    private static Vector2 MeasureExtent(ReadOnlySpan<GlyphRect> bounds)
    {
        var width = float.NegativeInfinity;
        var bottom = float.NegativeInfinity;
        foreach (ref readonly var bound in bounds)
        {
            // PenX + Advance (not the ink-based Right) so trailing whitespace still reserves its layout space.
            width = float.Max(width, bound.PenX + bound.Advance);
            bottom = float.Max(bottom, bound.Bottom);
        }

        return new Vector2(width, bottom);
    }

    private void MakeNewFont()
    {
        if (_fontManager.GetFont(FontFamily) is { } font) CurrentFont = font;
        InvalidateCharacterBounds();
        ReleaseLayouts();
        var layout = ComputeLayout(out var pending);
        if (pending) layout.Dispose();
        else _cachedLayouts = layout;
        InvalidateDesiredSize();
        InvalidateLayout();
    }


    /// <summary>
    ///     Every character's bounds at the current wrap, cached until invalidated.
    /// </summary>
    protected ReadOnlySpan<GlyphRect> GetCharacterBounds()
    {
        if (CurrentFont == null) return default;
        if (_cachedBounds is { } cached) return cached.AsSpan();

        var bounds = new PooledMemory<GlyphRect>(Content.Length);
        bounds.Truncate(MeasureContent(bounds.AsSpan(), Wrap));
        _cachedBounds = bounds;
        return bounds.AsSpan();
    }

    /// <summary>
    ///     Measures the content into <paramref name="into" />, which needs room for every character.
    /// </summary>
    protected int MeasureContent(Span<GlyphRect> into, float? wrap)
    {
        if (CurrentFont == null) return 0;
        return FontManager.MeasureText(CurrentFont, Content, FontSize, into, wrap ?? float.PositiveInfinity);
    }

    protected void InvalidateCharacterBounds()
    {
        _cachedBounds?.Dispose();
        _cachedBounds = null;
    }

    public override Vector2 ComputeDesiredContentSize()
    {
        if (Content.Empty() || CurrentFont == null) return new Vector2(0.0f, LineHeight);
        using var scratch = new PooledMemory<GlyphRect>(Content.Length);
        var bounds = scratch.AsSpan()[..MeasureContent(scratch.AsSpan(), null)];
        if (bounds.IsEmpty) return Vector2.Zero;

        var extent = MeasureExtent(bounds);
        var lines = Content.AsSpan().Count('\n') + 1;
        return extent with { Y = float.Max(extent.Y, LineHeight * lines) };
    }

    /// <summary>
    ///     The caller owns the returned buffer and must dispose it or cache it.
    /// </summary>
    protected PooledMemory<CachedQuadLayout> ComputeLayout(out bool anyPending)
    {
        if (CurrentFont == null || Content.Empty())
        {
            anyPending = false;
            return new PooledMemory<CachedQuadLayout>(0);
        }

        var pending = false;
        var bounds = GetCharacterBounds();
        var results = new PooledMemory<CachedQuadLayout>(bounds.Length);
        var count = 0;
        foreach (var bound in bounds)
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

            var (finalTransform, size) = MtsdfGlyphLayout.Compute(bound, glyph, range);

            results[count++] = new CachedQuadLayout(glyph.AtlasHandle, finalTransform, size, glyph.Coordinate, range);
        }

        anyPending = pending;
        results.Truncate(count);
        return results;
    }

    private void ReleaseLayouts()
    {
        _cachedLayouts?.Dispose();
        _cachedLayouts = null;
    }

    protected override void OnRemovedFromSurface(ISurface surface)
    {
        ReleaseLayouts();
        InvalidateCharacterBounds();
        base.OnRemovedFromSurface(surface);
    }

    public override void Dispose()
    {
        ReleaseLayouts();
        InvalidateCharacterBounds();
        base.Dispose();
    }
    
    private readonly List<Quad> _collectCacheQuads = [];
    private readonly List<ResourceHandle> _collectCacheAtlases = [];
    public override void CollectContent(in Matrix4x4 transform, CommandList commands)
    {
        _collectCacheQuads.Clear();
        _collectCacheAtlases.Clear();
        if (CurrentFont == null) return;
        if (Content.NotEmpty() && _cachedLayouts == null)
        {
            var layout = ComputeLayout(out var hadAnyPending);
            AddLayoutQuads(layout.AsSpan(), transform);
            if (hadAnyPending || layout.Count == 0) layout.Dispose();
            else _cachedLayouts = layout;
        }
        else if (_cachedLayouts is { } cached)
        {
            AddLayoutQuads(cached.AsSpan(), transform);
        }

        if (_collectCacheQuads.Count > 0)
        {
            commands.Add(new QuadDrawCommand(CollectionsMarshal.AsSpan(_collectCacheQuads),
                CollectionsMarshal.AsSpan(_collectCacheAtlases)));
        }
    }

    private void AddLayoutQuads(ReadOnlySpan<CachedQuadLayout> layouts, in Matrix4x4 transform)
    {
        foreach (ref readonly var layout in layouts)
        {
            _collectCacheQuads.Add(Quad.Mtsdf(layout.Atlas, layout.Transform * transform, layout.Size,
                layout.PixelRange, ForegroundColor, layout.Uv));
            if (!_collectCacheAtlases.Contains(layout.Atlas)) _collectCacheAtlases.Add(layout.Atlas);
        }
    }

    protected struct CachedQuadLayout(in ResourceHandle atlas, Matrix4x4 transform, Vector2 size, Vector4 uv,
        float pixelRange)
    {
        public readonly ResourceHandle Atlas = atlas;
        public Matrix4x4 Transform = transform;
        public Vector2 Size = size;
        public Vector4 Uv = uv;
        public float PixelRange = pixelRange;
    }
}
using System.Numerics;
using JetBrains.Annotations;
using Rin.Framework.Extensions;
using Rin.Framework.Math;
using Rin.Framework.Graphics.Textures;
using Rin.Framework.Views.Enums;
using Rin.Framework.Views.Font;
using Rin.Framework.Views.Graphics;
using Rin.Framework.Views.Graphics.Quads;
using Rin.Framework.Views.Sdf;

namespace Rin.Framework.Views.Content;

/// <summary>
///     Draw's text using an <see cref="MtsdfFont" />. Currently, hardcoded to
///     <a href="https://fonts.google.com/specimen/Roboto">Roboto</a>.
/// </summary>
public class TextBox : ContentView
{
    private GlyphRect[]? _cachedBounds;
    private CachedQuadLayout[]? _cachedLayouts;

    private string _content = string.Empty;
    private string _fontFamily = "Noto Sans";
    private IFontManager _fontManager = SApplication.Provider.Get<IFontManager>();
    private float _fontSize = 100.0f;
    private bool _wrapContent;
    protected float? Wrap;

    public TextBox()
    {
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
            _wrapContent = value;
            Invalidate(InvalidationType.Layout);
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
        Invalidate(InvalidationType.DesiredSize);
    }

    protected override Vector2 LayoutContent(in Vector2 availableSpace)
    {
        _cachedLayouts = null;
        Wrap = _wrapContent ? float.IsFinite(availableSpace.X) ? availableSpace.X + 2f : null : null;
        var bounds = GetCharacterBounds(Wrap).ToArray();
        if (bounds.Empty()) return Vector2.Zero;
        var width = bounds.MaxBy(c => c.Right).Right;
        var height = bounds.MaxBy(c => c.Bottom).Bottom;
        return new Vector2(width, height);
    }

    private void MakeNewFont()
    {
        if (_fontManager.GetFont(FontFamily) is { } font) CurrentFont = font;
        _cachedBounds = null;
        _cachedLayouts = ComputeLayout(out var pending);
        if (pending) _cachedLayouts = null;
        Invalidate(InvalidationType.DesiredSize);
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
        var width = bounds.Empty() ? 0 : bounds.Max(c => c.Right);
        var lines = float.Max(1, Content.Split("\n").Length);
        var height = LineHeight * lines;

        return new Vector2(width, height);
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

    public override void CollectContent(in Matrix4x4 transform, CommandList commands)
    {
        if (CurrentFont == null) return;
        if (Content.NotEmpty() && _cachedLayouts == null)
        {
            List<Quad> quads = [];

            var layout = ComputeLayout(out var hadAnyPending);
            var x4 = transform;
            quads.AddRange(layout.Select(c => Quad.Mtsdf(c.Atlas, c.Transform * x4, c.Size, Color.White,
                c.Uv)));
            if (quads.Count == 0) return;
            commands.Add(new QuadDrawCommand(quads));
            if (!hadAnyPending) _cachedLayouts = layout;
        }
        else if (_cachedLayouts != null)
        {
            var x4 = transform;
            commands.Add(new QuadDrawCommand(_cachedLayouts.Select(c =>
                Quad.Mtsdf(c.Atlas, c.Transform * x4, c.Size, Color.White, c.Uv))));
        }
    }

    protected struct CachedQuadLayout(in ImageHandle atlas, Matrix4x4 transform, Vector2 size, Vector4 uv)
    {
        public readonly ImageHandle Atlas = atlas;
        public Matrix4x4 Transform = transform;
        public Vector2 Size = size;
        public Vector4 Uv = uv;
    }
}
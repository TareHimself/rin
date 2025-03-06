using System.Numerics;
using JetBrains.Annotations;
using Rin.Engine.Core.Math;
using Rin.Engine.Views.Enums;
using Rin.Engine.Views.Font;
using Rin.Engine.Views.Graphics;
using Rin.Engine.Views.Graphics.Quads;
using Rin.Engine.Views.Sdf;
using Rin.Engine.Core.Extensions;
using SixLabors.Fonts;

namespace Rin.Engine.Views.Content;

internal struct CachedQuadLayout(int atlas, Mat3 transform, Vector2 size, Vector4 uv)
{
    public readonly int Atlas = atlas;
    public Mat3 Transform = transform;
    public Vector2 Size = size;
    public Vector4 Uv = uv;
}

/// <summary>
///     Draw's text using an <see cref="MtsdfFont" />. Currently, hardcoded to
///     <a href="https://fonts.google.com/specimen/Roboto">Roboto</a>.
/// </summary>
public class TextBox : ContentView
{
    private CharacterBounds[]? _cachedBounds;
    private CachedQuadLayout[]? _cachedLayouts;

    private string _content = string.Empty;

    private string _fontFamily = "Noto Sans";
    // protected float LineHeight => CurrentFont?.FontMetrics is { } metrics
    //     ? ((float)metrics.HorizontalMetrics.LineHeight / metrics.UnitsPerEm)
    //     : 0;

    private IFontManager _fontManager = SViewsModule.Get().GetFontManager();
    private float _fontSize = 100.0f;
    private FontStyle _fontStyle = FontStyle.Regular;
    private bool _wrapContent;
    protected float? Wrap;

    public TextBox()
    {
        _cachedLayouts = null;
        _cachedBounds = null;
        MakeNewFont();
    }

    [PublicAPI] protected SixLabors.Fonts.Font? CurrentFont { get; private set; }

    protected float LineHeight => CurrentFont?.FontMetrics is { } metrics
        ? metrics.HorizontalMetrics.AdvanceHeightMax * 64 / metrics.ScaleFactor * FontSize
        : 0;


    [PublicAPI] public Color ForegroundColor { get; set; } = Color.White;

    [PublicAPI] public Color BackgroundColor { get; set; } = Color.White.Clone(a: 0.0f);

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
    public FontStyle Style
    {
        get => _fontStyle;
        set
        {
            _fontStyle = value;
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
        _cachedLayouts = null;
        _cachedBounds = null;
        _content = newText;
        // TextRenderer.RenderTextTo();
        Invalidate(InvalidationType.DesiredSize);
    }

    protected override Vector2 LayoutContent(Vector2 availableSpace)
    {
        _cachedLayouts = null;
        Wrap = _wrapContent ? float.IsFinite(availableSpace.X) ? availableSpace.X : null : null;
        var bounds = GetCharacterBounds(Wrap).ToArray();
        var width = bounds.MaxBy(c => c.Right)?.Right ?? 0.0f;
        var height = bounds.MaxBy(c => c.Bottom)?.Bottom ?? 0.0f;
        return new Vector2(width, height);
    }

    private void MakeNewFont()
    {
        if (_fontManager.TryGetFont(FontFamily, out var family)) CurrentFont = family.CreateFont(FontSize, Style);
        _cachedBounds = null;
        _cachedLayouts = null;
        Invalidate(InvalidationType.DesiredSize);
    }


    protected IEnumerable<CharacterBounds> GetCharacterBounds(float? wrap = null, bool cache = true)
    {
        if (CurrentFont == null) return [];

        var opts = new TextOptions(CurrentFont)
        {
            WrappingLength = wrap == null ? -1.0f : (float)Math.Ceiling(wrap.Value + 10.0f)
        };

        var content = Content + "";

        TextMeasurer.TryMeasureCharacterBounds(content, opts, out var tempBounds);

        if (tempBounds.IsEmpty) return [];

        var allBounds = new Dictionary<int, FontRectangle>();
        var line = 0;

        foreach (var glyphBounds in tempBounds) allBounds.Add(glyphBounds.StringIndex, glyphBounds.Bounds);

        var result = content.Select((c, idx) =>
        {
            {
                if (allBounds.TryGetValue(idx, out var bounds))
                {
                    line = (int)Math.Floor(bounds.Y / LineHeight);
                    return new CharacterBounds(c, idx, bounds);
                }
            }

            if (c == '\n') line++;

            return new CharacterBounds(c, idx, 0, line * LineHeight + LineHeight / 2.0f, 0, 0);
        }).ToArray();

        return result;
    }

    protected override Vector2 ComputeDesiredContentSize()
    {
        if (Content.Empty() || CurrentFont == null) return new Vector2(0.0f, LineHeight);
        var last = GetCharacterBounds(cache: false).MaxBy(c => c.Right);
        var lines = Math.Max(1, Content.Split("\n").Length);
        var height = LineHeight * lines;

        return new Vector2(last?.Right ?? 0.0f, height);
    }

    public override void CollectContent(Mat3 transform, PassCommands commands)
    {
        if (CurrentFont == null) return;
        if (Content.NotEmpty() && _cachedLayouts == null)
        {
            List<CachedQuadLayout> layouts = [];
            List<Quad> quads = [];
            var hadAnyPending = false;
            foreach (var bound in GetCharacterBounds(Wrap))
            {
                var range = _fontManager.GetPixelRange();
                var glyph = _fontManager.GetGlyph(CurrentFont, bound.Character);

                if (glyph.State == LiveGlyphState.Invalid && bound.Character != ' ')
                {
                    _fontManager.Prepare(CurrentFont.Family, [bound.Character]);
                    hadAnyPending = true;
                }
                else if (glyph.State == LiveGlyphState.Pending)
                {
                    hadAnyPending = true;
                }

                if (glyph.State != LiveGlyphState.Ready) continue;

                var charOffset = new Vector2(bound.X, bound.Y);

                var size = new Vector2(bound.Width, bound.Height);
                var vectorSize = glyph.Size - new Vector2(range * 2);
                var scale = size / vectorSize;
                var pxRangeScaled = new Vector2(range) * scale;
                size += pxRangeScaled * 2;

                charOffset -= pxRangeScaled;
                //if (!charRect.IntersectsWith(drawInfo.Clip)) continue;
                //if(!charRect.Offset.Within(drawInfo.Clip) && !(charRect.Offset + charRect.Size).Within(drawInfo.Clip)) continue;

                var finalTransform = Mat3.Identity.Translate(charOffset)
                    .Translate(new Vector2(0.0f, size.Y)).Scale(new Vector2(1.0f, -1.0f));

                var layout = new CachedQuadLayout(glyph.AtlasId, finalTransform, size, glyph.Coordinate);
                layouts.Add(layout);
                quads.Add(Quad.Mtsdf(layout.Atlas, transform * layout.Transform, layout.Size, Color.White,
                    layout.Uv));
            }

            if (quads.Count == 0) return;
            commands.Add(new QuadDrawCommand(quads));
            if (!hadAnyPending) _cachedLayouts = layouts.ToArray();
        }
        else if (_cachedLayouts != null)
        {
            commands.Add(new QuadDrawCommand(_cachedLayouts.Select(c =>
                Quad.Mtsdf(c.Atlas, transform * c.Transform, c.Size, Color.White, c.Uv))));
        }
    }

    protected class CharacterBounds(char character, int contentIndex, float x, float y, float width, float height)
    {
        public readonly char Character = character;
        public readonly int ContentIndex = contentIndex;
        public readonly float Height = height;
        public readonly float Width = width;
        public readonly float X = x;
        public readonly float Y = y;

        public CharacterBounds(char character, int contentIndex, FontRectangle bounds) : this(character, contentIndex,
            bounds.X, bounds.Y, bounds.Width, bounds.Height)
        {
        }

        public float Right => X + Width;
        public float Left => X;
        public float Top => Y;
        public float Bottom => Y + Height;
    }
}
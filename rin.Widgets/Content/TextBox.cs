using System.Runtime.InteropServices;
using rin.Core.Extensions;
using rin.Graphics;
using rin.Core.Math;
using rin.Widgets.Graphics;
using rin.Widgets.Graphics.Quads;
using rin.Widgets.Sdf;
using rsl;
using SixLabors.Fonts;

namespace rin.Widgets.Content;

[StructLayout(LayoutKind.Sequential)]
internal struct TextOptionsDeviceBuffer
{
    public Vector4<float> bgColor;
    public Vector4<float> fgColor;
}

[StructLayout(LayoutKind.Sequential)]
public struct TextPushConstants
{
    public Matrix3 Transform;

    public Vector2<float> Size;

    public int atlasIdx;

    public Vector4<float> Rect;
}

/// <summary>
///     Draw's text using an <see cref="MtsdfFont" />. Currently, hardcoded to
///     <a href="https://fonts.google.com/specimen/Roboto">Roboto</a>.
/// </summary>
public class TextBox : Widget
{

    struct CachedQuadLayout(int atlas,Matrix3 transform, Vector2<float> size, Vector4<float> uv)
    {
        public int Atlas = atlas;
        public Matrix3 Transform = transform;
        public Vector2<float> Size = size;
        public Vector4<float> UV = uv;
    }
    private string _content;
    private Font? _latestFont;
    private SdfFont? _mtsdf;
    private List<CachedQuadLayout>? _cachedLayouts;
    private float _fontSize = 100.0f;

    protected float LineHeight => _latestFont?.FontMetrics is { } metrics
        ? (metrics.HorizontalMetrics.AdvanceHeightMax * 64 / metrics.ScaleFactor * FontSize)
        : 0;
    
    

    public TextBox(string inContent = "", float inFontSize = 100f,string fontFamily = "Arial")
    {
        FontSize = inFontSize;
        var gs = SGraphicsModule.Get();
        _content = inContent;
        _cachedLayouts = null;
        SWidgetsModule.Get().GetOrCreateFont(fontFamily).After(msdf =>
        {
            _mtsdf = msdf;
            
            MakeNewFont();
        });
    }


    public Color ForegroundColor { get; set; } = Color.White;

    public Color BackgroundColor { get; set; } = Color.White.Clone(a: 0.0f);
    
    public float FontSize
    {
        get => _fontSize;
        set
        {
            _cachedLayouts = null;
            _fontSize = value;
        }
    }
    

    public string Content
    {
        get => _content;
        set
        {
            var hasChanged = _content != value;
            if (hasChanged)
            {
                _cachedLayouts = null;
                TextChanged(value);
            }
            TryUpdateDesiredSize();
        }
    }

    protected virtual void TextChanged(string newText)
    {
        _content = newText;
    }

    protected bool FontReady => _mtsdf != null && _latestFont != null;
    
    protected override void OnDispose(bool isManual)
    {
        base.OnDispose(isManual);
        _mtsdf?.Dispose();
    }

    private void MakeNewFont()
    {
        _latestFont = _mtsdf?.GetFontFamily().CreateFont(FontSize, FontStyle.Regular);
        TryUpdateDesiredSize();
    }

    protected void GetContentBounds(out ReadOnlySpan<GlyphBounds> bounds)
    {
        if (_latestFont == null)
        {
            bounds = [];
            return;
        }
        var opts = new TextOptions(_latestFont);
        TextMeasurer.TryMeasureCharacterBounds(_content, opts, out var tempBounds);
        bounds = tempBounds;
    }

    protected override Vector2<float> ComputeDesiredContentSize()
    {
        if (Content.Empty() || _latestFont == null) return new Vector2<float>(0.0f,LineHeight);
        GetContentBounds(out var bounds);
        GlyphBounds? last = bounds.ToArray().MaxBy(c => c.Bounds.Right);
        var lines = Math.Max(1,Content.Split("\n").Length);
        var height = LineHeight * lines;

        return new Vector2<float>(last?.Bounds.Right ?? 0.0f, height);
    }

    public override void CollectContent(TransformInfo info, DrawCommands drawCommands)
    {
        if (!FontReady) return;
        if (Content.NotEmpty() && _cachedLayouts == null)
        {
            GetContentBounds(out var bounds);
            List<CachedQuadLayout> layouts = [];
            foreach (var bound in bounds)
            {
                var charInfo = _mtsdf?.GetGlyphInfo(_content[bound.StringIndex]);
                var atlasId = charInfo != null ? _mtsdf?.GetAtlasTextureId(charInfo.AtlasIdx) : null;
                if (charInfo == null || atlasId == null) continue;

                var charOffset = new Vector2<float>(bound.Bounds.X,
                    bound.Bounds.Y);
                var charSize = new Vector2<float>(bound.Bounds.Width, bound.Bounds.Height);
                //if (!charRect.IntersectsWith(drawInfo.Clip)) continue;
                //if(!charRect.Offset.Within(drawInfo.Clip) && !(charRect.Offset + charRect.Size).Within(drawInfo.Clip)) continue;

                var finalTransform = Matrix3.Identity.Translate(charOffset)
                    .Translate(new Vector2<float>(0.0f, charSize.Y)).Scale(new Vector2<float>(1.0f, -1.0f));
                var size = new Vector2<float>(bound.Bounds.Width, bound.Bounds.Height);
                var layout = new CachedQuadLayout(atlasId.Value, finalTransform, size, charInfo.Coordinates);
                layouts.Add(layout);
                drawCommands.AddSdf(layout.Atlas,info.Transform * layout.Transform,layout.Size,Color.White,layout.UV);
            }
            
            _cachedLayouts = layouts;
        }
        else if(_cachedLayouts != null)
        {
            drawCommands.Add(new QuadDrawCommand(_cachedLayouts.Select(c => Quad.NewSdf(c.Atlas,info.Transform * c.Transform,c.Size,Color.White,c.UV))));
        }
    }


   
}
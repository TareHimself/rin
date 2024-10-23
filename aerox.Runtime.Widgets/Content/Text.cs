using System.Runtime.InteropServices;
using aerox.Runtime.Extensions;
using aerox.Runtime.Graphics;
using aerox.Runtime.Math;
using aerox.Runtime.Widgets.Graphics;
using aerox.Runtime.Widgets.Mtsdf;
using SixLabors.Fonts;

namespace aerox.Runtime.Widgets.Content;

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
public class Text : Widget
{
    private string _content;
    private Font? _latestFont;
    private MtsdfFont? _mtsdf;

    public Text(string inContent = "", float inFontSize = 100f,string fontFamily = "Arial")
    {
        FontSize = inFontSize;
        var gs = SGraphicsModule.Get();
        _content = inContent;

        SWidgetsModule.Get().GetOrCreateFont(fontFamily).Then(msdf =>
        {
            _mtsdf = msdf;
            
            MakeNewFont();
            
            return Task.CompletedTask;
        });
    }


    public Color ForegroundColor { get; set; } = Color.White;

    public Color BackgroundColor { get; set; } = Color.White.Clone(a: 0.0f);


    public float FontSize { get; set; } = 100f;
    

    public string Content
    {
        get => _content;
        set
        {
            _content = value;
            CheckSize();
        }
    }

    protected bool ShouldDraw => _mtsdf != null && _latestFont != null;
    
    protected override void OnDispose(bool isManual)
    {
        base.OnDispose(isManual);
        _mtsdf?.Dispose();
    }

    private void MakeNewFont()
    {
        _latestFont = _mtsdf?.GetFontFamily().CreateFont(FontSize, FontStyle.Regular);
        CheckSize();
    }

    public void GetContentBounds(out ReadOnlySpan<GlyphBounds> bounds)
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

    protected override Size2d ComputeContentDesiredSize()
    {
        if (_latestFont == null) return new Size2d();
        var opts = new TextOptions(_latestFont);

        GetContentBounds(out var bounds);
        GlyphBounds? last = bounds.Length == 0 ? null : bounds[^1];
        var metrics = _latestFont.FontMetrics;
        var height = metrics.HorizontalMetrics.AdvanceHeightMax * 64 / metrics.ScaleFactor * FontSize;

        return new Size2d(last?.Bounds.Right ?? 0.0f, height);
    }

    public override void CollectContent(TransformInfo info, DrawCommands drawCommands)
    {
        if (!ShouldDraw) return;
        
        GetContentBounds(out var bounds);
        List<TextPushConstants> pushConstantsList = [];
        foreach (var bound in bounds)
        {
            var charInfo = _mtsdf?.GetGlyphInfo(_content[bound.StringIndex]);
            if (charInfo == null) continue;

            var charOffset = new Vector2<float>(bound.Bounds.X,
                bound.Bounds.Y);
            var charSize = new Size2d(bound.Bounds.Width, bound.Bounds.Height);

            //if (!charRect.IntersectsWith(drawInfo.Clip)) continue;
            //if(!charRect.Offset.Within(drawInfo.Clip) && !(charRect.Offset + charRect.Size).Within(drawInfo.Clip)) continue;

            var constants = new TextPushConstants
            {
                Transform = info.Transform.Translate(charOffset),
                Size = charSize,
                atlasIdx = charInfo.AtlasIdx,
                Rect = new Vector4<float>(charInfo.X,charInfo.Y,charInfo.Width,charInfo.Height)
            };

            pushConstantsList.Add(constants);
        }
    }
}
using System.Runtime.InteropServices;
using aerox.Runtime.Extensions;
using aerox.Runtime.Graphics;
using aerox.Runtime.Graphics.Material;
using aerox.Runtime.Math;
using SixLabors.Fonts;

namespace aerox.Runtime.Widgets.Defaults.Content;

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
    private readonly MaterialInstance _materialInstance;
    private readonly DeviceBuffer _optionsBuffer;
    private Color _backgroundColor = new(1.0f, 1.0f, 1.0f, 0.0f);
    private string _content;
    private Color _foregroundColor = new(1.0f);
    private Font? _latestFont;
    private MtsdfFont? _msdf;
    private bool _optionsDirty = true;

    public Text(string inContent = "", float inFontSize = 100f,string fontFamily = "Arial")
    {
        FontSize = inFontSize;
        var gs = SRuntime.Get().GetModule<SGraphicsModule>();
        _content = inContent;
        _optionsBuffer = gs.GetAllocator()
            .NewUniformBuffer<ImageOptionsDeviceBuffer>(debugName: "Image Options Buffer");
        _materialInstance = SWidgetsModule.CreateMaterial(Path.Join(SWidgetsModule.ShadersDir,"font.ash"));
        _materialInstance.BindBuffer("options", _optionsBuffer);

        SRuntime.Get().GetModule<SWidgetsModule>().GetOrCreateFont(fontFamily).Then(msdf =>
        {
            _msdf = msdf;

            if (_msdf != null) _materialInstance.BindTextureArray("TAtlas", _msdf.GetAtlases());

            MakeNewFont();


            return Task.CompletedTask;
        });
    }


    public Color ForegroundColor
    {
        get => _foregroundColor;
        set
        {
            _foregroundColor = value;
            _optionsDirty = true;
        }
    }

    public Color BackgroundColor
    {
        get => _backgroundColor;
        set
        {
            _backgroundColor = value;
            _optionsDirty = true;
        }
    }


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

    protected bool ShouldDraw => _msdf != null && _latestFont != null;

    protected override void OnAddedToRoot(WidgetSurface widgetSurface)
    {
        base.OnAddedToRoot(widgetSurface);
        _materialInstance.BindBuffer("ui", widgetSurface.GlobalBuffer);
    }

    protected override void OnDispose(bool isManual)
    {
        base.OnDispose(isManual);
        _optionsBuffer.Dispose();
        _materialInstance.Dispose();
        _msdf?.Dispose();
    }

    private void MakeNewFont()
    {
        _latestFont = _msdf?.GetFontFamily().CreateFont(FontSize, FontStyle.Regular);
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


    public override Size2d ComputeDesiredSize()
    {
        if (_latestFont == null) return new Size2d();
        var opts = new TextOptions(_latestFont);
        // TextMeasurer.TryMeasureCharacterBounds(_content,opts, out var bounds);
        GetContentBounds(out var bounds);
        GlyphBounds? last = bounds.Length == 0 ? null : bounds[^1];
        var metrics = _latestFont.FontMetrics;
        var height = metrics.HorizontalMetrics.AdvanceHeightMax * 64 / metrics.ScaleFactor * FontSize;

        return new Size2d(last?.Bounds.Right ?? 0.0f, height);
    }

    public override void Collect(WidgetFrame frame, DrawInfo info)
    {
        if (!ShouldDraw) return;

        if (_optionsDirty)
        {
            _optionsBuffer.Write(new TextOptionsDeviceBuffer
            {
                fgColor = _foregroundColor,
                bgColor = _backgroundColor
            });
            _optionsDirty = false;
        }

        //TextMeasurer.TryMeasureCharacterBounds(_content, new TextOptions(_latestFont), out var bounds);
        GetContentBounds(out var bounds);
        List<TextPushConstants> pushConstantsList = [];
        foreach (var bound in bounds)
        {
            var charInfo = _msdf?.GetCharInfo(_content[bound.StringIndex]);
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

        frame.AddCommand(new Draw.Commands.TextDrawCommand(_materialInstance, _msdf!, pushConstantsList.ToArray()));
    }
}
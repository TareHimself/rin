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

    public int textureIdx;
}

/// <summary>
///     Draw's text using an <see cref="MsdfFont" />. Currently, hardcoded to
///     <a href="https://fonts.google.com/specimen/Roboto">Roboto</a>.
/// </summary>
public class Text : Widget
{
    private const float MSDF_PADDING = 3.0f;
    private readonly MaterialInstance _materialInstance;
    private readonly DeviceBuffer _optionsBuffer;
    private Color _backgroundColor = new(1.0f, 1.0f, 1.0f, 0.0f);
    private string _content;
    private Color _foregroundColor = new(1.0f);
    private Font? _latestFont;
    private MsdfFont? _msdf;
    private bool _optionsDirty = true;

    public Text(string inContent = "", float inFontSize = 100f)
    {
        FontSize = inFontSize;
        var gs = Runtime.Instance.GetModule<GraphicsModule>();
        _content = inContent;
        _optionsBuffer = gs.GetAllocator()
            .NewUniformBuffer<ImageOptionsDeviceBuffer>(debugName: "Image Options Buffer");
        _materialInstance = WidgetsModule.CreateMaterial(
            gs.LoadShader(@$"{Runtime.SHADERS_DIR}\2d\rect.vert"),
            gs.LoadShader(@$"{Runtime.SHADERS_DIR}\2d\font.frag"));
        _materialInstance.BindBuffer("options", _optionsBuffer);

        Runtime.Instance.GetModule<WidgetsModule>().GetOrCreateFont("Arial").Then(msdf =>
        {
            _msdf = msdf;

            if (_msdf != null) _materialInstance.BindTextureArray("TAtlas", _msdf.GetTextures());

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

    public float ScaledPadding => FontSize / 32 * MSDF_PADDING;

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
        var height = ((metrics.HorizontalMetrics.AdvanceHeightMax * 64) / metrics.ScaleFactor) * FontSize;

        return new Size2d(last?.Bounds.Right ?? 0.0f, height);
    }

    public override void Draw(WidgetFrame frame, DrawInfo info)
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
            var textureIndex = _msdf?.GetTextureIndex(_content[bound.StringIndex]);
            if (textureIndex == null) continue;

            var charOffset = new Vector2<float>(bound.Bounds.X - ScaledPadding,
                bound.Bounds.Y - ScaledPadding);
            var charSize = new Size2d(bound.Bounds.Width + ScaledPadding, bound.Bounds.Height + ScaledPadding);

            //if (!charRect.IntersectsWith(drawInfo.Clip)) continue;
            //if(!charRect.Offset.Within(drawInfo.Clip) && !(charRect.Offset + charRect.Size).Within(drawInfo.Clip)) continue;

            var constants = new TextPushConstants
            {
                Transform = info.Transform.Translate(charOffset),
                Size = charSize,
                textureIdx = textureIndex.Value
            };
            
            pushConstantsList.Add(constants);
        }

        frame.AddCommand(new Draw.Commands.Text(_materialInstance, _msdf!, pushConstantsList.ToArray()));
    }
}
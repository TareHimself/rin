using System.Numerics;
using System.Text;
using Examples.Common.Views;
using Rin.Core.Views;
using Rin.Core.Views.Composite;
using Rin.Core.Views.Content;
using Rin.Core.Views.Layouts;

namespace Examples.TextInput.Views;

/// <summary>
///     Two text fields over a readout of what the focused one holds.
/// </summary>
public sealed class TextInputDemoView : RectView
{
    private const float CardWidth = 560f;
    private const float BlockHeight = 96f;
    private const int MaxShownUnits = 16;

    private readonly ProbeInputView _line = new() { Label = "Single line", Placeholder = "Type here" };

    private readonly ProbeInputView _block = new()
    {
        Label = "Multiple lines",
        Placeholder = "Enter adds a line",
        Multiline = true,
        WrapContent = true
    };

    private readonly TextBoxView _field = Ui.Label(string.Empty, 14f, Ui.Muted);
    private readonly TextBoxView _caret = Ui.Label(string.Empty, 14f, Ui.Muted);
    private readonly TextBoxView _composing = Ui.Label(string.Empty, 14f, Ui.Muted);
    private readonly TextBoxView _units = new()
    {
        FontSize = 14f,
        ForegroundColor = Ui.Muted,
        WrapContent = true
    };
    private readonly StringBuilder _unitText = new();

    public TextInputDemoView()
    {
        Color = Ui.Background;
        _line.Changed += Show;
        _block.Changed += Show;
        ShowNothing();

        InitChild = new PanelView
        {
            InitSlots =
            [
                new PanelSlot
                {
                    Child = BuildCard(),
                    MinAnchor = new Vector2(0.5f),
                    MaxAnchor = new Vector2(0.5f),
                    Alignment = new Vector2(0.5f),
                    SizeToContent = true
                }
            ]
        };
    }

    private CardView BuildCard()
    {
        return new CardView
        {
            Color = Ui.Surface,
            BorderColor = Ui.Border,
            ShadowAlpha = 0.5f,
            BorderRadius = BorderRadius.All(22f),
            Padding = new Padding(36f),
            InitChild = new SizerView
            {
                WidthOverride = CardWidth,
                InitChild = Ui.Column(
                    Ui.Slot(Ui.Label("Text input", 36f)),
                    Ui.Slot(Ui.Gap(8f)),
                    Ui.Slot(Ui.Label("Click a field and type. An input method composes in place.", 15f, Ui.Muted)),
                    Ui.Slot(Ui.Gap(28f)),
                    Ui.Slot(Ui.Label(_line.Label, 13f, Ui.Muted)),
                    Ui.Slot(Ui.Gap(8f)),
                    Ui.Slot(Ui.Input(_line)),
                    Ui.Slot(Ui.Gap(18f)),
                    Ui.Slot(Ui.Label(_block.Label, 13f, Ui.Muted)),
                    Ui.Slot(Ui.Gap(8f)),
                    Ui.Slot(new SizerView { HeightOverride = BlockHeight, InitChild = Ui.Input(_block) }),
                    Ui.Slot(Ui.Gap(26f)),
                    Ui.Slot(_field),
                    Ui.Slot(Ui.Gap(6f)),
                    Ui.Slot(_caret),
                    Ui.Slot(Ui.Gap(6f)),
                    Ui.Slot(_composing),
                    Ui.Slot(Ui.Gap(6f)),
                    Ui.Slot(_units))
            }
        };
    }

    private void ShowNothing()
    {
        _field.Content = "Field: none focused";
        _caret.Content = "Caret: -";
        _composing.Content = "Composing: -";
        _units.Content = "UTF-16 units: -";
    }

    private void Show(ProbeInputView input)
    {
        _field.Content = $"Field: {input.Label}";
        _caret.Content = $"Caret: after unit {input.CursorPosition + 1} of {input.Content.Length}";
        _composing.Content = input.Composing.Length > 0 ? $"Composing: {input.Composing}" : "Composing: -";
        _units.Content = FormatUnits(input.Content);
    }

    private string FormatUnits(string content)
    {
        _unitText.Clear().Append("UTF-16 units:");
        var first = int.Max(0, content.Length - MaxShownUnits);
        if (first > 0) _unitText.Append(" ...");
        for (var i = first; i < content.Length; i++) _unitText.Append(' ').Append(((int)content[i]).ToString("X4"));
        if (content.Length == 0) _unitText.Append(" -");
        return _unitText.ToString();
    }
}

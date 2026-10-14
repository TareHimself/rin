using Examples.AudioPlayer.Views;
using Examples.Common.Views;
using Rin.Core.Views;
using Rin.Core.Views.Composite;
using Rin.Core.Views.Content;
using Rin.Core.Views.Layouts;

namespace Examples.Paint.Views;

/// <summary>
///     The toolbar (swatches, brush size, undo and clear) above the canvas.
/// </summary>
public sealed class PaintView : RectView
{
    private const float MinBrush = 2f;
    private const float MaxBrush = 48f;

    private static readonly Color[] Palette =
    [
        Color.Black,
        Color.FromRgb(0.95f, 0.95f, 0.95f),
        Color.FromRgb(0.91f, 0.26f, 0.21f),
        Color.FromRgb(0.98f, 0.6f, 0.15f),
        Color.FromRgb(0.98f, 0.85f, 0.25f),
        Color.FromRgb(0.3f, 0.75f, 0.4f),
        Color.FromRgb(0.25f, 0.55f, 0.95f),
        Color.FromRgb(0.6f, 0.4f, 0.9f),
        Color.FromRgb(0.95f, 0.5f, 0.75f),
        Color.FromRgb(0.45f, 0.3f, 0.2f)
    ];

    private readonly TextBoxView _sizeLabel;
    private int _selected;

    public CanvasView Canvas { get; } = new();

    public PaintView()
    {
        Color = Ui.Background;
        Padding = new Padding(16f);
        _sizeLabel = Ui.Label(SizeText(), 14f, Ui.Muted);
        InitChild = Ui.Column(
            Ui.Slot(BuildToolbar(), fit: CrossFit.Available),
            Ui.Slot(Ui.Gap(12f)),
            Ui.Slot(Canvas, 1f, CrossFit.Available));
    }

    private FlexBoxView BuildToolbar()
    {
        var row = Ui.Row();
        for (var i = 0; i < Palette.Length; i++)
        {
            var index = i;
            row.Add(Ui.Slot(new SwatchView(Palette[index], () => _selected == index, () => Select(index)),
                fit: CrossFit.Desired, align: CrossAlign.Center));
            row.Add(Ui.Slot(Ui.Gap(0f, 6f)));
        }

        row.Add(Ui.Slot(Ui.Gap(0f, 18f)));
        row.Add(Ui.Slot(Ui.Label("Size", 14f, Ui.Muted), fit: CrossFit.Desired, align: CrossAlign.Center));
        row.Add(Ui.Slot(Ui.Gap(0f, 10f)));
        row.Add(Ui.Slot(new SizerView
        {
            WidthOverride = 180f,
            InitChild = new SliderView(GetBrushFraction, SetBrushFraction) { LiveUpdate = true, KnobAlwaysVisible = true }
        }, fit: CrossFit.Desired, align: CrossAlign.Center));
        row.Add(Ui.Slot(Ui.Gap(0f, 10f)));
        row.Add(Ui.Slot(new SizerView { WidthOverride = 44f, InitChild = _sizeLabel }, fit: CrossFit.Desired,
            align: CrossAlign.Center));
        row.Add(Ui.Slot(Ui.Gap(0f), 1f));
        row.Add(Ui.Slot(Ui.QuietButton("Undo", Canvas.Undo), fit: CrossFit.Desired, align: CrossAlign.Center));
        row.Add(Ui.Slot(Ui.Gap(0f, 8f)));
        row.Add(Ui.Slot(Ui.DangerButton("Clear", Canvas.Clear), fit: CrossFit.Desired, align: CrossAlign.Center));
        return row;
    }

    private void Select(int index)
    {
        _selected = index;
        Canvas.BrushColor = Palette[index];
    }

    private float GetBrushFraction()
    {
        return (Canvas.BrushSize - MinBrush) / (MaxBrush - MinBrush);
    }

    private void SetBrushFraction(float fraction)
    {
        Canvas.BrushSize = float.Round(MinBrush + fraction * (MaxBrush - MinBrush));
        _sizeLabel.Content = SizeText();
    }

    private string SizeText()
    {
        return $"{Canvas.BrushSize:0} px";
    }
}

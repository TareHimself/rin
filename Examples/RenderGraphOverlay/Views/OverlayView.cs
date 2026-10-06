using System.Numerics;
using Rin.Core.Views;
using Rin.Core.Views.Composite;
using Rin.Core.Views.Content;
using Rin.Core.Views.Layouts;
using RenderGraphOverlay.Snapshot;

namespace RenderGraphOverlay.Views;

public sealed class OverlayView : PanelView
{
    private const float ToggleMargin = 16f;

    private readonly TextBoxView _toggleLabel = new() { FontSize = 15f };
    private readonly ModalPanelView _window;

    public OverlayView(GraphSnapshotService service, Vector2 corner, bool startOpen)
    {
        Visibility = Visibility.VisibleNoHitTestSelf;

        _window = new ModalPanelView
        {
            Color = new Color(0.1f, 0.11f, 0.14f, 0.97f),
            BorderRadius = new Vector4(14f),
            Clip = Clip.Bounds,
            InitChild = new InspectorView(service)
        };

        InitSlots =
        [
            new PanelSlot
            {
                Child = _window,
                MinAnchor = new Vector2(0.06f),
                MaxAnchor = new Vector2(0.94f, 0.86f)
            },
            new PanelSlot
            {
                Child = MakeToggleButton(),
                SizeToContent = true,
                Anchor = corner,
                Alignment = corner,
                Offset = new Vector2(corner.X > 0.5f ? -ToggleMargin : ToggleMargin,
                    corner.Y > 0.5f ? -ToggleMargin : ToggleMargin)
            }
        ];

        SetOpen(startOpen);
    }

    public bool IsOpen { get; private set; }

    public void SetOpen(bool open)
    {
        IsOpen = open;
        _window.Visibility = open ? Visibility.Visible : Visibility.Collapsed;
        _toggleLabel.Content = open ? "Close graph" : "Render graph";
    }

    private ButtonView MakeToggleButton()
    {
        var button = new ButtonView
        {
            Color = new Color(0.22f, 0.42f, 0.82f, 0.95f),
            BorderRadius = new Vector4(10f),
            Padding = new Padding(12f, 7f),
            InitChild = _toggleLabel
        };
        button.OnReleased += (_, _) => SetOpen(!IsOpen);
        return button;
    }
}

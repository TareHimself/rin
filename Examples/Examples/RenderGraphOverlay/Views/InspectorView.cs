using System.Numerics;
using Rin.Core.Views;
using Rin.Core.Views.Composite;
using Rin.Core.Views.Content;
using Rin.Core.Views.Layouts;
using Examples.RenderGraphOverlay.Snapshot;

namespace Examples.RenderGraphOverlay.Views;

public sealed class InspectorView : PanelView
{
    private const float PanelMargin = 16f;

    private readonly GraphCanvasView _canvas = new();

    private readonly TextBoxView _details = new()
    {
        FontSize = 14f,
        WrapContent = true,
        Content = DetailsFormatter.Format(null, null)
    };

    private readonly TextBoxView _edgesLabel = new() { FontSize = 15f, Content = EdgesLabel(false) };
    private readonly GraphSnapshotService _service;

    private readonly TextBoxView _status = new()
    {
        FontSize = 14f,
        ForegroundColor = GraphPalette.Muted,
        Content = "Press Capture to snapshot the render graph"
    };

    private GraphSnapshot? _snapshot;

    public InspectorView(GraphSnapshotService service)
    {
        _service = service;
        _canvas.OnSelectionChanged += _ => RefreshDetails();

        InitSlots =
        [
            new PanelSlot
            {
                Child = new RectView
                {
                    Color = new Color(0.1f, 0.11f, 0.14f, 1f),
                    Clip = Clip.Bounds,
                    InitChild = _canvas
                },
                MinAnchor = Vector2.Zero,
                MaxAnchor = Vector2.One
            },
            new PanelSlot
            {
                Child = MakeToolbar(),
                SizeToContent = true,
                Anchor = Vector2.Zero,
                Offset = new Vector2(PanelMargin)
            },
            new PanelSlot
            {
                Child = MakeDetailsPanel(),
                SizeToContent = true,
                Anchor = new Vector2(1f, 0f),
                Alignment = new Vector2(1f, 0f),
                Offset = new Vector2(-PanelMargin, PanelMargin)
            },
            new PanelSlot
            {
                Child = new LegendView(),
                SizeToContent = true,
                Anchor = new Vector2(0f, 1f),
                Alignment = new Vector2(0f, 1f),
                Offset = new Vector2(PanelMargin, -PanelMargin)
            }
        ];
    }

    public override void Update(float deltaTime)
    {
        base.Update(deltaTime);
        if (_service.TakeResult() is { } result) Apply(result);
    }

    private void Apply(CaptureResult result)
    {
        if (result.Snapshot is not { } snapshot)
        {
            _status.Content = $"Capture failed: {result.Error}";
            return;
        }

        _snapshot = snapshot;
        _canvas.SetSnapshot(snapshot);
        _status.Content = $"{snapshot.PassCount} passes, {snapshot.PrunedPassCount} pruned, {snapshot.CapturedAt:T}";
        RefreshDetails();
    }

    private void RefreshDetails()
    {
        _details.Content = DetailsFormatter.Format(_canvas.SelectedPass, _snapshot);
    }

    private IView MakeToolbar()
    {
        return new RectView
        {
            Color = Color.Black with { A = 0.7f },
            BorderRadius = new Vector4(12f),
            Padding = new Padding(10f),
            InitChild = new ListView(Axis.Row)
            {
                InitSlots =
                [
                    new ListSlot { Child = MakeButton("Capture", _service.Request) },
                    new ListSlot { Child = new SizerView { WidthOverride = 8f } },
                    new ListSlot { Child = MakeButton("Fit", _canvas.FitToView) },
                    new ListSlot { Child = new SizerView { WidthOverride = 8f } },
                    new ListSlot { Child = MakeEdgesToggle() },
                    new ListSlot { Child = new SizerView { WidthOverride = 14f } },
                    new ListSlot { Child = _status, Align = CrossAlign.Center }
                ]
            }
        };
    }

    private static IView MakeButton(string text, Action onClick)
    {
        return MakeButton(new TextBoxView { Content = text, FontSize = 15f }, onClick);
    }

    private static IView MakeButton(IView content, Action onClick)
    {
        var button = new ButtonView
        {
            Color = new Color(0.22f, 0.42f, 0.82f, 1f),
            BorderRadius = new Vector4(8f),
            Padding = new Padding(14f, 6f),
            InitChild = content
        };
        button.OnReleased += (_, _) => onClick();
        return button;
    }

    private IView MakeEdgesToggle()
    {
        return MakeButton(_edgesLabel, () =>
        {
            _canvas.ShowAllEdges = !_canvas.ShowAllEdges;
            _edgesLabel.Content = EdgesLabel(_canvas.ShowAllEdges);
        });
    }

    private static string EdgesLabel(bool showAll)
    {
        return showAll ? "Edges: all" : "Edges: reduced";
    }

    private IView MakeDetailsPanel()
    {
        return new BackgroundBlurView
        {
            Strength = 6f,
            InitChild = new RectView
            {
                Color = Color.Black with { A = 0.6f },
                BorderRadius = new Vector4(12f),
                Padding = new Padding(14f),
                InitChild = new SizerView
                {
                    WidthOverride = 420f,
                    HeightOverride = 480f,
                    InitChild = new ScrollListView
                    {
                        Axis = Axis.Column,
                        InitSlots = [new ListSlot { Child = _details, Fit = CrossFit.Available }]
                    }
                }
            }
        };
    }
}

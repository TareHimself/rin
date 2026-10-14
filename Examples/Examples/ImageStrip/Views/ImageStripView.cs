using System.Numerics;
using Examples.Common.Views;
using Rin.Core;
using Rin.Core.Extensions;
using Rin.Core.Views;
using Rin.Core.Views.Composite;
using Rin.Core.Views.Content;
using Rin.Core.Views.Graphics.Quads;
using Rin.Core.Views.Layouts;

namespace Examples.ImageStrip.Views;

/// <summary>
///     A row of tiles that widen on hover: images, a custom shader and a color wheel.
/// </summary>
public sealed class ImageStripView : PanelView
{
    private const float TileWidth = 150f;
    private const float TileHeight = 480f;
    private const float TileRadius = 30f;
    private const float ButtonWidth = 170f;

    private readonly ListView _strip = new() { Axis = Axis.Row };
    private readonly TextBoxView _hint = Ui.Label("Add a tile, or drop image files on the window.", 18f, Ui.Muted);

    public ImageStripView()
    {
        InitSlots =
        [
            new PanelSlot
            {
                Child = new RectView { Color = Ui.Background },
                MinAnchor = Vector2.Zero,
                MaxAnchor = Vector2.One
            },
            new PanelSlot
            {
                Child = _hint,
                MinAnchor = new Vector2(0.5f),
                MaxAnchor = new Vector2(0.5f),
                Alignment = new Vector2(0.5f),
                SizeToContent = true
            },
            new PanelSlot
            {
                Child = _strip,
                MinAnchor = new Vector2(0.5f),
                MaxAnchor = new Vector2(0.5f),
                Alignment = new Vector2(0.5f),
                SizeToContent = true
            },
            new PanelSlot
            {
                Child = BuildBar(),
                Offset = new Vector2(16f),
                SizeToContent = true
            }
        ];
    }

    private CardView BuildBar()
    {
        return new CardView
        {
            Color = Ui.Surface,
            BorderColor = Ui.Border,
            ShadowAlpha = 0.4f,
            BorderRadius = BorderRadius.All(16f),
            Padding = new Padding(12f),
            InitChild = Ui.Row(
                Ui.Slot(Fixed(Ui.PrimaryButton("Add images", PickImages))),
                Ui.Slot(Ui.Gap(0f, 10f)),
                Ui.Slot(Fixed(Ui.QuietButton("Add shader", AddShader))),
                Ui.Slot(Ui.Gap(0f, 10f)),
                Ui.Slot(Fixed(Ui.QuietButton("Add color wheel", AddColorWheel))))
        };
    }

    private static SizerView Fixed(IView button)
    {
        return new SizerView { WidthOverride = ButtonWidth, InitChild = button };
    }

    public void AddImages(string[] paths)
    {
        foreach (var path in paths) AddTile(new AsyncFileImageView(path) { BorderRadius = BorderRadius.All(TileRadius) });
    }

    private void PickImages()
    {
        Task.Run(() => IApplication.Get().SelectFile("Select images", true, "*.png;*.jpg;*.jpeg"))
            .DispatchMain(AddImages);
    }

    private void AddShader()
    {
        AddTile(new PrettyView { BorderRadius = BorderRadius.All(TileRadius) });
    }

    private void AddColorWheel()
    {
        AddTile(new CanvasView
        {
            Paint = (canvas, transform, commands) =>
            {
                var wheel = Quad.Rect(transform, canvas.GetContentSize());
                wheel.Mode = Quad.RenderMode.ColorWheel;
                commands.AddQuads(wheel);
            }
        });
    }

    private void AddTile(IView content)
    {
        _hint.Visibility = Visibility.Hidden;
        _strip.Add(new ExpandOnHoverView
        {
            WidthOverride = TileWidth,
            HeightOverride = TileHeight,
            InitChild = content,
            Padding = 10f
        });
    }
}

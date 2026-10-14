using System.Numerics;
using Examples.Common.Views;
using Rin.Core.Graphics;
using Rin.Core.Graphics.Windows;
using Rin.Core.Views;
using Rin.Core.Views.Composite;
using Rin.Core.Views.Content;
using Rin.Core.Views.Layouts;

namespace Examples.ChildWindows.Views;

/// <summary>
///     Buttons that open child windows and toggle fullscreen on the main one.
/// </summary>
public sealed class WindowControlsView : RectView
{
    private const float CardWidth = 420f;
    private static readonly Extent2D ChildSize = new(500);

    private readonly IWindow _window;
    private readonly TextBoxView _status = Ui.Label(string.Empty, 14f, Ui.Muted);
    private readonly List<IWindow> _children = [];
    private int _opened;

    public WindowControlsView(IWindow window)
    {
        _window = window;
        Color = Ui.Background;
        ShowStatus();

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
                    Ui.Slot(Ui.Label("Windows", 36f)),
                    Ui.Slot(Ui.Gap(8f)),
                    Ui.Slot(Ui.Label("Closing a child window disposes it.", 15f, Ui.Muted)),
                    Ui.Slot(Ui.Gap(28f)),
                    Ui.Slot(Ui.PrimaryButton("Open a child window", OpenChild)),
                    Ui.Slot(Ui.Gap(12f)),
                    Ui.Slot(Ui.QuietButton("Toggle fullscreen (Alt+Enter)", ToggleFullscreen)),
                    Ui.Slot(Ui.Gap(22f)),
                    Ui.Slot(_status))
            }
        };
    }

    private void OpenChild()
    {
        _opened++;
        var child = _window.CreateChild($"Child {_opened}", ChildSize);
        _children.Add(child);
        child.OnDispose += () =>
        {
            _children.Remove(child);
            ShowStatus();
        };
        ShowStatus();
    }

    public override void Dispose()
    {
        base.Dispose();
        foreach (var child in _children.ToArray()) child.Dispose();
    }

    private void ToggleFullscreen()
    {
        _window.SetFullscreen(!_window.IsFullscreen);
    }

    private void ShowStatus()
    {
        _status.Content = $"Child windows open: {_children.Count}";
    }
}

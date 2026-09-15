using System.Numerics;
using Examples.Common;
using Rin.Core;
using Rin.Core.Graphics;
using Rin.Core.Graphics.Windows;
using Rin.Core.Views;
using Rin.Core.Views.Composite;
using Rin.Core.Views.Content;
using Rin.Core.Views.Layouts;
using Rin.Core.Views.Window;

namespace experiment.FontIcon;

/// <summary>
///     Smoke test for <see cref="FontIconView" />: loads Font Awesome 6 Free's solid icon font (a plain OTF/TTF
///     whose icon glyphs sit at Private-Use-Area codepoints) through the ordinary <see cref="IFontManager" />
///     pipeline and draws a grid of icons through the MTSDF renderer - no icon-specific rendering code involved.
/// </summary>
public class FontIconApplication : ExampleApplication
{
    // Codepoints from Font Awesome 6 Free's metadata (icons.yml), "solid" style.
    private static readonly (string Name, char Codepoint)[] Icons =
    [
        ("star", (char)0xf005),
        ("heart", (char)0xf004),
        ("house", (char)0xf015),
        ("user", (char)0xf007),
        ("gear", (char)0xf013),
        ("check", (char)0xf00c),
        ("xmark", (char)0xf00d),
        ("bell", (char)0xf0f3),
        ("camera", (char)0xf030),
        ("music", (char)0xf001),
        ("magnifying-glass", (char)0xf002),
        ("envelope", (char)0xf0e0),
        ("thumbs-up", (char)0xf164),
        ("cloud", (char)0xf0c2),
        ("bolt", (char)0xf0e7)
    ];

    private static readonly (string Label, Color Color)[] TintOptions =
    [
        ("white", Color.White),
        ("orange", new Color(1.0f, 0.55f, 0.1f, 1.0f)),
        ("cyan", new Color(0.2f, 0.85f, 0.95f, 1.0f))
    ];

    private string _iconFontFamily = "";

    protected override void OnStartup()
    {
        IViewsModule.Get().AddFont("FontIcon/Fonts/fa-solid-900.ttf");

        // The family name embedded in the font's name table can vary by release ("Font Awesome 6 Free",
        // "Font Awesome 6 Free Solid", ...) - find it instead of guessing.
        var iconFont = IViewsModule.Get().FontManager.GetFonts()
            .FirstOrDefault(f => f.Name.Contains("Awesome", StringComparison.OrdinalIgnoreCase));
        if (iconFont is null) throw new InvalidOperationException("Font Awesome font failed to load");
        _iconFontFamily = iconFont.Name;

        IGraphicsModule.Get().OnWindowCreated += window => { window.OnClose += _ => RequestExit(); };
        IViewsModule.Get().OnSurfaceCreated += SetupDemo;

        IGraphicsModule.Get().CreateWindow("Font Icon Test", new Extent2D(1320, 720),
            WindowFlags.Visible | WindowFlags.Resizable);
    }

    protected override void OnShutdown()
    {
    }

    private void SetupDemo(IWindowSurface surf)
    {
        var rows = new ListView(Axis.Column) { Padding = new Padding(24.0f) };

        foreach (var (label, color) in TintOptions)
        {
            var row = new ListView(Axis.Row) { Padding = new Padding { Bottom = 12.0f } };
            foreach (var (name, codepoint) in Icons) row.Add(BuildCard(name, codepoint, color));
            rows.Add(new TextBoxView { Content = label, FontSize = 18 });
            rows.Add(row);
        }

        surf.Add(new PanelView
        {
            InitSlots =
            [
                new PanelSlot
                {
                    Child = rows,
                    MinAnchor = Vector2.Zero,
                    MaxAnchor = Vector2.Zero,
                    SizeToContent = true
                }
            ]
        });
    }

    private IView BuildCard(string name, char codepoint, Color tint)
    {
        var contents = new ListView(Axis.Column)
        {
            InitSlots =
            [
                new ListSlot
                {
                    Align = CrossAlign.Center,
                    Child = new FontIconView
                    {
                        FontFamily = _iconFontFamily,
                        Icon = codepoint,
                        IconSize = 40.0f,
                        Color = tint
                    }
                },
                new ListSlot
                {
                    Align = CrossAlign.Center,
                    Child = new TextBoxView
                    {
                        Content = name,
                        FontSize = 12,
                        ForegroundColor = Color.White with { A = 0.6f }
                    }
                }
            ]
        };

        return new RectView
        {
            InitChild = contents,
            Color = new Color(0.15f, 0.15f, 0.18f, 1.0f),
            BorderRadius = new Vector4(8.0f),
            Padding = new Padding(12.0f)
        };
    }
}

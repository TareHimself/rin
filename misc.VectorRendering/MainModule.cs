using System.Numerics;
using Rin.Engine;
using Rin.Engine.Graphics;
using Rin.Engine.Views;
using Rin.Engine.Views.Composite;
using Rin.Engine.Views.Content;
using Rin.Engine.Views.Font;
using Rin.Engine.Views.Graphics;
using Rin.Engine.Views.Layouts;
using rin.Examples.Common.Views;
using SixLabors.Fonts;

namespace misc.VectorRendering;

[Module(typeof(SViewsModule))]
[AlwaysLoad]
public class MainModule : IModule
{
    private readonly IFontManager _fontManager = new DefaultFontManager();

    private Font _font;

    public void Start(SEngine engine)
    {
        // _fontManager.LoadSystemFonts();
        // if (!_fontManager.TryGetFont("Noto Sans JP", out var family)) return;
        // var font = family.CreateFont(100, FontStyle.Regular);
        // _font = font;
        // SGraphicsModule.Get().OnWindowCreated += window =>
        // {
        //     window.OnClose += _ =>
        //     {
        //         if (window.Parent != null)
        //             window.Dispose();
        //         else
        //             SEngine.Get().RequestExit();
        //     };
        // };
        //
        // var window = SGraphicsModule.Get().CreateWindow(500, 500, "Test Window");
        // var surface = window.GetViewSurface();
        // surface?.Add(new Panel
        // {
        //     Slots =
        //     [
        //         new PanelSlot
        //         {
        //             Child = new AsyncWebImage(
        //                 "https://applescoop.org/image/wallpapers/mac/samurai-in-a-field-of-flowers-with-mountains-landscape-hdr-anime-style-best-most-popular-free-download-wallpapers-for-macbook-pro-and-macbook-air-and-microsoft-windows-desktop-pcs-4k-07-12-2024-1733638427-hd-wallpaper.png"),
        //             MinAnchor = Vector2.Zero,
        //             MaxAnchor = new Vector2(1.0f)
        //         },
        //         new PanelSlot
        //         {
        //             Child = new BackgroundBlur
        //             {
        //                 Radius = 10,
        //                 Strength = 5,
        //                 Child = new Panel
        //                 {
        //                     Slots =
        //                     [
        //                         new PanelSlot
        //                         {
        //                             // Child = new TextBox
        //                             // {
        //                             //     Content = "A",
        //                             //     FontSize = 700
        //                             //     //FontFamily = "Noto Sans JP"
        //                             // },
        //                             Child = new Canvas
        //                             {
        //                                 Paint = PaintCanvas
        //                             },
        //                             Alignment = new Vector2(0.5f),
        //                             MinAnchor = new Vector2(0.5f),
        //                             MaxAnchor = new Vector2(0.5f),
        //                             SizeToContent = true
        //                         }
        //                     ]
        //                 }
        //             },
        //             MinAnchor = Vector2.Zero,
        //             MaxAnchor = new Vector2(1.0f)
        //         }
        //     ]
        // });
    }

    public void Stop(SEngine engine)
    {
    }

    public static MainModule Get()
    {
        return SEngine.Get().GetModule<MainModule>();
    }

    // public void PaintCanvas(Canvas target, Mat3 transform, CommandList cmds)
    // {
    //     var str = "A B C"; //"素早い茶色のキツネ";
    //     // var glyph = _fontManager.GetGlyph(_font, 'A', out TODO);
    //     var opts = new TextOptions(_font);
    //     //TextMeasurer.TryMeasureCharacterBounds(str, opts, out var bounds);
    //
    //
    //     //var renderer = new ViewsTextRenderer(cmds);
    //     TextRenderer.RenderTextTo(new ViewsTextRenderer(cmds), "Hello World", new TextOptions(_font));
    //     //cmds.Add(new PathCommand(transform, _font,str));
    // }
}
using System.Numerics;
using Rin.Engine;
using Rin.Engine.Audio;
using Rin.Engine.Graphics;
using Rin.Engine.Graphics.Windows;
using Rin.Engine.Views;
using Rin.Engine.Views.Composite;
using Rin.Engine.Views.Layouts;
using rin.Examples.AudioPlayer.Views;
using rin.Examples.Common.Views;
using SpotifyExplode;
using YoutubeExplode;

namespace rin.Examples.AudioPlayer;

[Module(typeof(SViewsModule), typeof(SAudioModule))]
[AlwaysLoad]
public class SAudioPlayer : IModule, ISingletonGetter<SAudioPlayer>
{
    public readonly SpotifyClient SpClient = new();
    public readonly YoutubeClient YtClient = new();

    public void Start(SEngine engine)
    {
        // {
        //     var manager = SViewsModule.Get().GetFontManager();
        //     if (manager.TryGetFont("Noto Sans", out var family))
        //     {
        //         manager
        //             .PrepareAtlas(family,Enumerable.Range(32,127).Select(c => (char)c).Where(c => c.IsPrintable())).Wait();
        //     }
        // }
        SAudioModule.Get().SetVolume(0.1f);
        var window = SGraphicsModule.Get().CreateWindow(500, 500, "Rin Audio Player");
        window.OnClose += _ => { SEngine.Get().RequestExit(); };
        Backgrounds(window);
        var surf = SViewsModule.Get().GetWindowSurface(window);
        surf?.Add(new MainPanel());
    }

    public void Stop(SEngine engine)
    {
    }

    public static SAudioPlayer Get()
    {
        return SEngine.Get().GetModule<SAudioPlayer>();
    }

    public void Backgrounds(IWindow window)
    {
        var surf = SViewsModule.Get().GetWindowSurface(window);

        if (surf == null) return;

        var panel = surf.Add(new Panel());

        var switcher = new Switcher();
        panel.Add(
            new PanelSlot
            {
                Child = switcher,
                MaxAnchor = new Vector2(1.0f)
            }
        );

        surf.Window.OnKey += e =>
        {
            if (e is { State: InputState.Pressed, Key: InputKey.Left })
            {
                if (switcher.SelectedIndex - 1 < 0) return;
                switcher.SelectedIndex -= 1;
                return;
            }

            if (e is { State: InputState.Pressed, Key: InputKey.Right })
            {
                if (switcher.SelectedIndex + 1 >= switcher.GetSlots().Count()) return;
                switcher.SelectedIndex += 1;
                return;
            }

            if (e is { State: InputState.Pressed, Key: InputKey.Enter })
            {
                var p = Platform.SelectFile("Select Images", filter: "*.png;*.jpg;*.jpeg", multiple: true);
                foreach (var path in p)
                    switcher.Add(new Fitter
                    {
                        Child = new AsyncFileImage(path),
                        FittingMode = FitMode.Cover
                    });
            }
        };
    }
}
using System.Numerics;
using Rin.Framework;
using Rin.Framework.Audio;
using Rin.Framework.Graphics;
using Rin.Framework.Graphics.Windows;
using Rin.Framework.Views;
using Rin.Framework.Views.Composite;
using Rin.Framework.Views.Layouts;
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

    public void Start(SApplication application)
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
        var window = SGraphicsModule.Get().CreateWindow("Rin Audio Player", new Extent2D(500));
        window.OnClose += _ => { SApplication.Get().RequestExit(); };
        Backgrounds(window);
        var surf = SViewsModule.Get().GetWindowSurface(window);
        surf?.Add(new MainPanel());
    }

    public void Stop(SApplication application)
    {
    }

    public static SAudioPlayer Get()
    {
        return SApplication.Get().GetModule<SAudioPlayer>();
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
using AudioPlayer.Views;
using rin.Framework.Views;
using rin.Framework.Views.Composite;
using rin.Framework.Audio;
using rin.Framework.Core;
using rin.Framework.Graphics;
using rin.Framework.Graphics.Windows;
using rin.Framework.Views.Layouts;
using SpotifyExplode;
using YoutubeExplode;
using Utils = rin.Framework.Graphics.Utils;

namespace AudioPlayer;

[Module(typeof(SViewsModule), typeof(SAudioModule)),AlwaysLoad]
public class SAudioPlayer : IModule, ISingletonGetter<SAudioPlayer>
{
    public readonly SpotifyClient SpClient = new SpotifyClient();
    public readonly YoutubeClient YtClient = new YoutubeClient();

    public void Startup(SRuntime runtime)
    {
        Utils.RunWindowsOnTick();
        Utils.RunDrawOnTick();
        
        SAudioModule.Get().SetVolume(0.1f);
        var window = SGraphicsModule.Get().CreateWindow(500, 500, "Rin Audio Player");
        window.OnCloseRequested += (_) => { SRuntime.Get().RequestExit(); };
        Backgrounds(window);
        var surf = SViewsModule.Get().GetWindowSurface(window);
        surf?.Add(new MainPanel());
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
                MaxAnchor = 1.0f
            }
        );

        // panel.AddChild(new WCBlur
        // {
        //     Tint = new Color(1.0f)
        //     {
        //         R = 0.3f,
        //         G = 0.3f,
        //         B = 0.3f
        //     }
        // })?.Mutate(slot =>
        // {
        //     slot.MinAnchor = 0.0f;
        //     slot.MaxAnchor = 1.0f;
        // });

        surf.Window.OnKey += (e) =>
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

    public void Shutdown(SRuntime runtime)
    {

    }

    public static SAudioPlayer Get() => SRuntime.Get().GetModule<SAudioPlayer>();
}
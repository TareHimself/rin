using aerox.Runtime;
using aerox.Runtime.Audio;
using aerox.Runtime.DataStructures;
using aerox.Runtime.Widgets;
using aerox.Runtime.Widgets.Containers;
using aerox.Runtime.Widgets.Content;
using aerox.Runtime.Windows;
using AudioPlayer.Widgets;
using SpotifyExplode;
using YoutubeExplode;

namespace AudioPlayer;

[RuntimeModule(typeof(SWidgetsModule),typeof(SAudioModule))]
public class SAudioPlayer : RuntimeModule, ISingletonGetter<SAudioPlayer>
{
    public SpotifyClient SpClient = new SpotifyClient();
    public override void Startup(SRuntime runtime)
    {
        base.Startup(runtime);
        SAudioModule.Get().SetVolume(50);
        SWindowsModule.Get().CreateWindow(500, 500, "Audio Player");
        Backgrounds();
        var surf = SWidgetsModule.Get().GetWindowSurface();
        if(surf == null) return;
        surf.Add(new MainPanel());
    }
    
    public void Backgrounds()
    {
        var surf = SWidgetsModule.Get().GetWindowSurface();

        if (surf == null) return;

        var panel = surf.Add(new Panel());

        var switcher = new Switcher();
        panel.AddChild(
            new PanelSlot(switcher)
            {
                MaxAnchor = 1.0f
            }
        );

        // panel.AddChild(new BackgroundBlur
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
            if (e is { State: KeyState.Pressed, Key: Key.Left })
            {
                if (switcher.SelectedIndex - 1 < 0) return;
                switcher.SelectedIndex -= 1;
                return;
            }

            if (e is { State: KeyState.Pressed, Key: Key.Right })
            {
                if (switcher.SelectedIndex + 1 >= switcher.GetNumSlots()) return;
                switcher.SelectedIndex += 1;
                return;
            }

            if (e is { State: KeyState.Pressed, Key: Key.Enter })
            {
                var p = Platform.SelectFile("Select Images", filter: "*.png;*.jpg;*.jpeg", multiple: true);
                foreach (var path in p)
                    switcher.AddChild(new Fitter(new AsyncFileImage(path))
                    {
                        FittingMode = FitMode.Cover
                    });
            }
        };
    }

    public override void Shutdown(SRuntime runtime)
    {
        base.Shutdown(runtime);
    }

    public static SAudioPlayer Get() => SRuntime.Get().GetModule<SAudioPlayer>();
}
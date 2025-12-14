using System.Numerics;
using rin.Examples.AudioPlayer.Views;
using rin.Examples.Common;
using rin.Examples.Common.Views;
using Rin.Framework;
using Rin.Framework.Audio;
using Rin.Framework.Graphics;
using Rin.Framework.Graphics.Windows;
using Rin.Framework.Views;
using Rin.Framework.Views.Composite;
using Rin.Framework.Views.Layouts;
using SpotifyExplode;
using YoutubeExplode;

namespace rin.Examples.AudioPlayer;

public class AudioPlayerApp : ExampleApplication
{
    public readonly SpotifyClient SpClient = new();
    public readonly YoutubeClient YtClient = new();

    protected override void OnStartup()
    {
        IAudioModule.Get().SetVolume(0.1f);
        var window = IGraphicsModule.Get().CreateWindow("Rin Audio Player", new Extent2D(500));
        window.OnClose += _ => { RequestExit(); };
        Backgrounds(window);
        var surf = IViewsModule.Get().GetWindowSurface(window);
        surf?.Add(new MainPanelView());
    }

    protected override void OnShutdown()
    {
        
    }
    
    public void Backgrounds(IWindow window)
    {
        var surf = IViewsModule.Get().GetWindowSurface(window);

        if (surf == null) return;

        var panel = surf.Add(new PanelView());

        var switcher = new SwitcherView();
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
                    switcher.Add(new FitterView
                    {
                        Child = new AsyncFileImageView(path),
                        FittingMode = FitMode.Cover
                    });
            }
        };
    }
}
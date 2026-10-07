using System.Numerics;
using Examples.AudioPlayer.Views;
using Examples.Common;
using Examples.Common.Views;
using Rin.Core;
using Rin.Core.Audio;
using Rin.Core.Graphics;
using Rin.Core.Graphics.Windows;
using Rin.Core.Views;
using Rin.Core.Views.Composite;
using Rin.Core.Views.Layouts;
using Rin.Core.Views.Window;
using SpotifyExplode;
using YoutubeExplode;

namespace Examples.AudioPlayer;

public sealed class AudioPlayerExample : Example
{
    public static readonly SpotifyClient SpClient = new();
    public static readonly YoutubeClient YtClient = new();

    public override string Name => "audio-player";

    public override string Title => "Audio Player";

    public override Extent2D WindowSize => new(500);

    public override void Start(ExampleContext context)
    {
        IAudioModule.Get().SetVolume(0.1f);
        Backgrounds(context.Surface);
        context.Surface.Add(new MainPanelView());
    }

    private static void Backgrounds(IWindowSurface surf)
    {
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
                if (switcher.SelectedIndex + 1 >= switcher.SlotCount) return;
                switcher.SelectedIndex += 1;
                return;
            }

            if (e is { State: InputState.Pressed, Key: InputKey.Enter })
            {
                var p = IApplication.Get().SelectFile("Select Images", filter: "*.png;*.jpg;*.jpeg", multiple: true);
                foreach (var path in p)
                    switcher.Add(new FitterView
                    {
                        InitChild = new AsyncFileImageView(path),
                        FittingMode = FitMode.Cover
                    });
            }
        };
    }
}
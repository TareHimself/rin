using Examples.AudioPlayer.Views;
using Rin.Core.Audio;
using Rin.Core.Graphics;
using Rin.Core.Graphics.Windows;

namespace Examples.AudioPlayer;

public sealed class AudioPlayerExample : Example
{
    private const float StartVolume = 0.4f;

    public override string Name => "audio-player";

    public override string Title => "Audio Player";

    public override Extent2D WindowSize => new(1180, 760);

    public override void Start(ExampleContext context)
    {
        IAudioModule.Get().SetVolume(StartVolume);
        Icons.Load();

        var player = context.Add(new PlayerView());
        player.AddFiles(context.Args);
        context.OnDrop(e => player.AddFiles(e.Paths));
        context.OnKey(e =>
        {
            if (e.State != InputState.Pressed) return;

            switch (e.Key)
            {
                case InputKey.Space:
                    player.TogglePlay();
                    break;
                case InputKey.Left:
                    player.Previous();
                    break;
                case InputKey.Right:
                    player.Next();
                    break;
                case InputKey.V:
                    player.NextVisualizer();
                    break;
            }
        });
    }
}

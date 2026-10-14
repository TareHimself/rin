using Examples.AudioEffects.Views;
using Rin.Core.Audio;
using Rin.Core.Graphics;

namespace Examples.AudioEffects;

public sealed class AudioEffectsExample : Example
{
    public override string Name => "audio-effects";

    public override string Title => "Audio Effects";

    public override Extent2D WindowSize => new(820, 760);

    public override void Start(ExampleContext context)
    {
        var master = IAudioModule.Get().MasterAudioGroup;
        master.Volume = 0.5f;
        context.Add(new AudioEffectsView(master));
    }
}

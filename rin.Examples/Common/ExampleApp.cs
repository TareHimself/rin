using rin.Framework;
using rin.Framework.Audio;
using rin.Framework.Audio.BassAudio;
using rin.Framework.Graphics;
using rin.Framework.Views;

namespace rin.Examples.Common;

public class ExampleApp : App
{
    public override IGraphicsModule GraphicsModule { get; } = new GraphicsModule();
    public override IAudioModule AudioModule { get; } = new BassAudioModule();
    public override IViewsModule ViewsModule { get; }

    protected override void Update(float deltaSeconds)
    {
        throw new NotImplementedException();
    }
}
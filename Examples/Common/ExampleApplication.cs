using Rin.Audio.Miniaudio;
using Rin.Core;
using Rin.Core.Audio;
using Rin.Core.Graphics;
using Rin.Graphics.Vulkan;
using Rin.Core.Views;

namespace Examples.Common;

public abstract class ExampleApplication : Application
{
    public TextureCache Textures { get; } = new();

    protected override void OnShutdown()
    {
        Textures.Dispose();
    }

    public override IGraphicsModule CreateGraphicsModule()
    {
        return new VulkanGraphicsModule();
    }

    public override IViewsModule CreateViewsModule()
    {
        return new ViewsModule();
    }

    public override IAudioModule CreateAudioModule()
    {
        return new MiniaudioAudioModule();
    }
}
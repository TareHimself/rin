using Rin.Audio.Miniaudio;
using Rin.Core;
using Rin.Core.Audio;
using Rin.Core.Graphics;
using Rin.Graphics.Vulkan;
using Rin.Core.Views;

namespace rin.Examples.Common;

public abstract class ExampleApplication : Application
{
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
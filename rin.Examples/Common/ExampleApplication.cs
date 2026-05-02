using Rin.Framework;
using Rin.Framework.Audio;
using Rin.Framework.Audio.Bass;
using Rin.Framework.Graphics;
using Rin.Framework.Graphics.Vulkan;
using Rin.Framework.Views;

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
        return new BassAudioModule();
    }
}
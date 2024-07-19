using TerraFX.Interop.Vulkan;

namespace aerox.Runtime.Graphics;

public class DefaultFrameState : FrameState
{
    public override void Apply(Frame frame)
    {
        var cmd = frame.GetCommandBuffer();
        cmd
            .SetRasterizerDiscard(false)
            .DisableMultiSampling();
    }
}
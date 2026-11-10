using JetBrains.Annotations;
using Rin.Core.Graphics;
using Rin.Shade;

namespace Rin.World.Graphics.Default.Shaders;

[Shader]
public partial class CullingShader : Shader
{
    public struct PushConstants
    {
        public DeviceSpan<UpdatableBounds3D> Bounds;
        public uint InvocationCount;
        public DeviceSpan<uint> Output;
    }

    [Push] [UsedImplicitly] protected PushConstants Push;

    [Compute(64, 1, 1)]
    public void Compute(ComputeIn input)
    {
        var index = input.ThreadId;
        if (index >= Push.InvocationCount) return;

        Push.Output[index] = 1u;
    }
}

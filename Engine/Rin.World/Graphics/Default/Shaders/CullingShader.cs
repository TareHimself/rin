using JetBrains.Annotations;
using Rin.Shade;

namespace Rin.World.Graphics.Default.Shaders;

[Shader("Shaders/Rin/World/Mesh/Compute/culling.slang")]
public partial class CullingShader : Shader
{
    public struct PushConstants
    {
        public BufferRef<UpdatableBounds3D> Bounds;
        public uint InvocationCount;
        public BufferRef<uint> Output;
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

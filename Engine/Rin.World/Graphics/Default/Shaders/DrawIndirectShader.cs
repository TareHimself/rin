using JetBrains.Annotations;
using Rin.Core.Graphics;
using Rin.Shade;

namespace Rin.World.Graphics.Default.Shaders;

[Shader]
public partial class DrawIndirectShader : Shader
{
    public struct PushConstants
    {
        public DeviceSpan<uint> CullingResults;
        public DeviceSpan<IndirectMeshRecord> Meshes;
        public uint InvocationCount;
        public DeviceSpan<DrawIndexedIndirectCommand> Output;
        public DeviceSpan<uint> DrawCount;
    }

    [Push] [UsedImplicitly] protected PushConstants Push;

    [Compute(64, 1, 1)]
    public void Compute(ComputeIn input)
    {
        var index = input.ThreadId;
        if (index >= Push.InvocationCount) return;

        var mesh = Push.Meshes[index];
        var wasCulled = Push.CullingResults[mesh.MeshIndex] == 0u;
        if (wasCulled) return;

        var drawIndex = Push.DrawCount.InterlockedAdd(0u, 1u);
        Push.Output[drawIndex] = ToCommand(mesh);
    }

    private static DrawIndexedIndirectCommand ToCommand(IndirectMeshRecord mesh)
    {
        DrawIndexedIndirectCommand command;
        command.IndexCount = mesh.IndicesCount;
        command.InstanceCount = 1u;
        command.FirstIndex = mesh.IndicesStart;
        command.VertexOffset = (int)mesh.VertexStart;
        command.FirstInstance = mesh.Instance;
        return command;
    }
}

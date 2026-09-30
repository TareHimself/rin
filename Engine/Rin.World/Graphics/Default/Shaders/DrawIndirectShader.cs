using JetBrains.Annotations;
using Rin.Shade;

namespace Rin.World.Graphics.Default.Shaders;

[Shader("Shaders/Rin/World/Mesh/Compute/draw_indirect.slang")]
public partial class DrawIndirectShader : Shader
{
    public struct PushConstants
    {
        public BufferRef<uint> CullingResults;
        public BufferRef<IndirectMeshRecord> Meshes;
        public uint InvocationCount;
        public BufferRef<DrawIndexedIndirectCommand> Output;
        public BufferRef<uint> DrawCount;
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

        uint drawIndex = 0u;
        Intrinsics.InterlockedAdd(ref Push.DrawCount[0], 1u, out drawIndex);
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

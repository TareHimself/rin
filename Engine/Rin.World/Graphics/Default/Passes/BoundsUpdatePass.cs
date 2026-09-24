using JetBrains.Annotations;
using Rin.Core.Graphics;
using Rin.Core.Graphics.Graph;
using Rin.Core.Graphics.Shaders;

namespace Rin.World.Graphics.Default.Passes;

/// <summary>
///  Updates the bounds of skinned meshes
/// </summary>
/// <param name="collectedData"></param>
public partial class BoundsUpdatePass(DefaultWorldCollectedData collectedData) : IComputePass
{
    [ComputeShader("Shaders/World/Mesh/Compute/bounds_update.slang")]
    private partial IComputeShader Shader { get; }

    /// <summary>
    /// Buffer for holding <see cref="SkinnedMesh"/>
    /// </summary>
    private uint SkinnedMeshBuffers { get; set; }

    public uint Id { get; set; }

    public void Configure(IGraphConfig config)
    {
        // config.ReadBuffer(collectedData.SkinningOutputBufferId,
        //     GraphBufferUsage.Compute); // All skinned meshes use one output buffer
        // config.WriteBuffer(collectedData.BoundsBufferId, GraphBufferUsage.Compute);
        // SkinnedMeshBuffers =
        //     config.CreateBuffer<SkinnedMesh>(collectedData.SkinnedSurfaceCount, GraphBufferUsage.HostThenCompute);
    }

    public void Execute(ICompiledGraph graph, IExecutionContext ctx)
    {
        // var boundsBuffer = graph.GetBufferOrException(collectedData.BoundsBufferId);
        // var skinnedMeshBuffer = graph.GetBufferOrException(SkinnedMeshBuffers);
        // ulong offset = 0;
        // for (var i = 0; i < collectedData.SkinnedSurfaceCount; i++)
        // {
        //     var mesh = collectedData.ProcessedMeshes[collectedData.SkinnedMeshStartIndex + i];
        //     offset += skinnedMeshBuffer.WriteSingle(mesh.VertexBuffer.GetAddress(), offset);
        // }
        //
        // if (Shader.Bind(ctx) is not { } bindContext) return;
        //
        // bindContext
        //     .Push(new Push
        //     {
        //         SkinnedMeshesAddress = skinnedMeshBuffer.GetAddress(),
        //         TotalInvocations = collectedData.SkinnedSurfaceCount,
        //         BoundsBufferAddress = boundsBuffer.GetAddress()
        //     })
        //     .Invoke((uint)collectedData.SkinnedSurfaceCount);
    }

    [NoReorder]
    private struct Push
    {
        public required ulong SkinnedMeshesAddress;
        public required int TotalInvocations;
        public required ulong BoundsBufferAddress;
    }

    [NoReorder]
    private struct SkinnedMesh
    {
        public required int BoundsIndex;
        public required ulong VertexBuffer;
        public required uint VertexCount;
    }
}
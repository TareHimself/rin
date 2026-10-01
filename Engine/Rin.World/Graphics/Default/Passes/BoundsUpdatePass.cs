using Rin.Core.Graphics;
using Rin.Core.Graphics.Graph;
using Rin.Core.Graphics.Shaders;
using Rin.Core.Shared;
using Rin.Shade;
using Rin.World.Graphics.Default.Shaders;
using Rin.World.Graphics.Mesh;

namespace Rin.World.Graphics.Default.Passes;

/// <summary>
///     Recomputes the bounds of skinned surfaces from their skinned vertices, replacing the rest-pose bounds that
///     <see cref="InitSceneResourcesPass" /> wrote. Runs after <see cref="SkinningPass" />.
/// </summary>
/// <param name="sceneFrame"></param>
public partial class BoundsUpdatePass(DefaultSceneFrame sceneFrame) : IComputePass
{
    [ComputeShader<BoundsUpdateShader>]
    private partial IComputeShader Shader { get; }

    private uint SkinnedMeshBufferId { get; set; }

    public uint Id { get; set; }

    public void Configure(IGraphConfig config)
    {
        config.ReadBuffer(sceneFrame.SkinningOutputBufferId, GraphBufferUsage.Compute);
        config.WriteBuffer(sceneFrame.BoundsBufferId, GraphBufferUsage.Compute);
        SkinnedMeshBufferId =
            config.CreateBuffer<BoundsUpdateShader.SkinnedMesh>(sceneFrame.ProcessedSkinnedMeshCount, GraphBufferUsage.HostThenCompute);
    }

    public void Execute(ICompiledGraph graph, IExecutionContext ctx)
    {
        var skinnedCount = sceneFrame.ProcessedSkinnedMeshCount;
        if (skinnedCount == 0) return;

        var boundsBuffer = graph.GetBufferOrException(sceneFrame.BoundsBufferId);
        var skinnedMeshBuffer = graph.GetBufferOrException(SkinnedMeshBufferId);

        using (var skinnedMeshes = new PooledMemory<BoundsUpdateShader.SkinnedMesh>(skinnedCount))
        {
            for (var i = 0; i < skinnedCount; i++)
            {
                var mesh = sceneFrame.ProcessedMeshes[sceneFrame.SkinnedMeshStartIndex + i];
                skinnedMeshes[i] = new BoundsUpdateShader.SkinnedMesh
                {
                    Index = mesh.AbsoluteMeshIndex,
                    Vertices = new BufferRef<Vertex>(mesh.VertexBuffer.GetAddress()),
                    Count = (uint)mesh.Surface.VertexCount
                };
            }

            skinnedMeshBuffer.Write(skinnedMeshes);
        }

        if (Shader.Bind(ctx) is not { } bindContext) return;

        bindContext
            .Push(new BoundsUpdateShader.PushConstants
            {
                SkinnedMeshes = new BufferRef<BoundsUpdateShader.SkinnedMesh>(skinnedMeshBuffer.GetAddress()),
                TotalInvocations = skinnedCount,
                Output = new BufferRef<UpdatableBounds3D>(boundsBuffer.GetAddress())
            })
            .Invoke((uint)skinnedCount);
    }
}

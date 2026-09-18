using JetBrains.Annotations;
using Rin.Core.Graphics;
using Rin.Core.Graphics.Graph;
using Rin.Core.Graphics.Shaders;

namespace Rin.World.Graphics.Default.Passes;

/// <summary>
///     Updates the bounds of skinned meshes
/// </summary>
/// <param name="renderContext"></param>
public partial class BoundsUpdatePass(DefaultWorldRenderContext renderContext) : IComputePass
{
    [ComputeShader("Shaders/World/Mesh/Compute/bounds_update.slang")]
    private partial IComputeShader Shader { get; }

    private int _skinnedMeshCount;

    private uint SkinnedMeshBufferId { get; set; }
    public uint Id { get; set; }
    public bool IsTerminal => false;
    public Action? OnPrune => null;

    public void Configure(IGraphConfig config)
    {
        _skinnedMeshCount = renderContext.ProcessedSkinnedMeshes.Length;
        config.ReadBuffer(renderContext.SkinningOutputBufferId,
            GraphBufferUsage.Compute); // All skinned meshes use one output buffer
        config.ReadBuffer(renderContext.BoundsBufferId, GraphBufferUsage.Compute);
        SkinnedMeshBufferId = config.CreateBuffer<SkinnedMesh>(_skinnedMeshCount, GraphBufferUsage.HostThenCompute);
    }

    public void Execute(ICompiledGraph graph, IExecutionContext ctx)
    {
        var boundsBuffer = graph.GetBufferOrException(renderContext.BoundsBufferId);
        var skinnedMeshBuffer = graph.GetBufferOrException(SkinnedMeshBufferId);
        skinnedMeshBuffer.Write(renderContext.ProcessedSkinnedMeshes.Select(c => new SkinnedMesh
        {
            MeshId = c.Id,
            VertexBuffer = c.VertexBuffer.GetAddress(),
            VertexCount = c.VertexCount
        }).ToArray());

        if (Shader.Bind(ctx) is not { } bindContext) return;

        bindContext
            .Push(new Push
            {
                SkinnedMeshesAddress = skinnedMeshBuffer.GetAddress(),
                TotalInvocations = _skinnedMeshCount,
                BoundsBufferAddress = boundsBuffer.GetAddress()
            })
            .Invoke((uint)_skinnedMeshCount);
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
        public required int MeshId;
        public required ulong VertexBuffer;
        public required uint VertexCount;
    }
}
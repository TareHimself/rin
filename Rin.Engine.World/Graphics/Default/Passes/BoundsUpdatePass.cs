using Rin.Engine.Graphics;
using Rin.Engine.Graphics.FrameGraph;
using Rin.Engine.Graphics.Shaders;

namespace Rin.Engine.World.Graphics.Default.Passes;

/// <summary>
///     Updates the bounds of skinned meshes
/// </summary>
/// <param name="renderContext"></param>
public class BoundsUpdatePass(DefaultWorldRenderContext renderContext) : IComputePass
{
    private readonly IComputeShader _shader = SGraphicsModule
        .Get()
        .MakeCompute("World/Shaders/Mesh/Compute/bounds_update.slang");

    private int _skinnedMeshCount;

    private uint SkinnedMeshBufferId { get; set; }
    public uint Id { get; set; }
    public bool IsTerminal => false;

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
        }));

        if (!_shader.Bind(ctx)) return;

        _shader.Push(ctx,
            new Push
            {
                SkinnedMeshesAddress = skinnedMeshBuffer.GetAddress(),
                TotalInvocations = _skinnedMeshCount,
                BoundsBufferAddress = boundsBuffer.GetAddress()
            });

        ctx.Invoke(_shader, (uint)_skinnedMeshCount);
    }


    private struct Push
    {
        public required ulong SkinnedMeshesAddress;
        public required int TotalInvocations;
        public required ulong BoundsBufferAddress;
    }

    private struct SkinnedMesh
    {
        public required int MeshId;
        public required ulong VertexBuffer;
        public required uint VertexCount;
    }
}
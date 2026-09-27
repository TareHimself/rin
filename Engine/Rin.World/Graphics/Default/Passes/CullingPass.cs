using JetBrains.Annotations;
using Rin.Core.Graphics;
using Rin.Core.Graphics.Graph;
using Rin.Core.Graphics.Shaders;

namespace Rin.World.Graphics.Default.Passes;

/// <summary>
///     Culls all meshes based on the main view (Will be updated to support a view index in the future)
///     writes results to <see cref="CullingPass.OutputBufferId" />
/// </summary>
/// <param name="collectedData"></param>
public partial class CullingPass(DefaultWorldViewData collectedData) : IComputePass
{
    [ComputeShader("Shaders/World/Mesh/Compute/culling.slang")]
    private partial IComputeShader Shader { get; }

    [PublicAPI] public uint OutputBufferId { get; set; }

    public uint Id { get; set; }

    public void Configure(IGraphConfig config)
    {
        // Always create OutputBufferId (GraphConfig.CreateBuffer clamps zero size to 1) - downstream
        // passes like FillIndirectBuffersPass unconditionally read it regardless of mesh count.
        config.ReadBuffer(collectedData.SceneFrame.BoundsBufferId, GraphBufferUsage.Compute);
        OutputBufferId = config.CreateBuffer<uint>(collectedData.SceneFrame.TotalMeshCount, GraphBufferUsage.Compute);
    }

    public void Execute(ICompiledGraph graph, IExecutionContext ctx)
    {
        if (collectedData.SceneFrame.TotalMeshCount == 0) return; // nothing to cull; a zero-sized dispatch isn't valid

        var boundsBuffer = graph.GetBufferOrException(collectedData.SceneFrame.BoundsBufferId);
        var outputBuffer = graph.GetBufferOrException(OutputBufferId);

        if (Shader.Bind(ctx) is not { } bindContext) return;
        bindContext
            .Push(new Push
            {
                BoundsBufferAddress = boundsBuffer.GetAddress(),
                TotalInvocations = collectedData.SceneFrame.TotalMeshCount,
                OutputBufferAddress = outputBuffer.GetAddress()
            })
            .Invoke((uint)collectedData.SceneFrame.TotalMeshCount);
    }

    [NoReorder]
    private struct Push
    {
        public required ulong BoundsBufferAddress;
        public required int TotalInvocations;
        public required ulong OutputBufferAddress;
    }
}
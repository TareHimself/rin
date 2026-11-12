using JetBrains.Annotations;
using Rin.Core.Graphics;
using Rin.Core.Graphics.Graph;
using Rin.Core.Graphics.Shaders;
using Rin.Shade;
using Rin.World.Graphics.Default.Shaders;

namespace Rin.World.Graphics.Default.Passes;

/// <summary>
///     Culls all meshes against the main view and writes the results to <see cref="CullingPass.OutputBufferId" />
/// </summary>
public partial class CullingPass(DefaultWorldViewData collectedData) : IComputePass
{
    [ComputeShader<CullingShader>]
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
            .Push(new CullingShader.PushConstants
            {
                Bounds = boundsBuffer.ToDeviceSpan<UpdatableBounds3D>(),
                InvocationCount = (uint)collectedData.SceneFrame.TotalMeshCount,
                Output = outputBuffer.ToDeviceSpan<uint>()
            })
            .Invoke((uint)collectedData.SceneFrame.TotalMeshCount);
    }
}
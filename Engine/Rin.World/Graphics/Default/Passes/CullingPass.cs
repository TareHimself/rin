using JetBrains.Annotations;
using Rin.Core.Graphics;
using Rin.Core.Graphics.Graph;
using Rin.Core.Graphics.Shaders;

namespace Rin.World.Graphics.Default.Passes;

/// <summary>
///     Culls all meshes based on the main view (Will be updated to support a view index in the future)
///     writes results to <see cref="CullingPass.OutputBufferId" />
/// </summary>
/// <param name="renderContext"></param>
public partial class CullingPass(DefaultWorldRenderContext renderContext) : IComputePass
{
    [ComputeShader("Shaders/World/Mesh/Compute/culling.slang")]
    private partial IComputeShader Shader { get; }

    [PublicAPI] public uint OutputBufferId { get; set; }

    public uint Id { get; set; }
    public bool IsTerminal => false;
    public Action? OnPrune => null;

    public void Configure(IGraphConfig config)
    {
        config.ReadBuffer(renderContext.BoundsBufferId, GraphBufferUsage.Compute);
        OutputBufferId = config.CreateBuffer<uint>(renderContext.TotalMeshCount, GraphBufferUsage.Compute);
    }

    public void Execute(ICompiledGraph graph, IExecutionContext ctx)
    {
        var boundsBuffer = graph.GetBufferOrException(renderContext.BoundsBufferId);
        var outputBuffer = graph.GetBufferOrException(OutputBufferId);

        if (Shader.Bind(ctx) is not { } bindContext) return;
        bindContext
            .Push(new Push
            {
                BoundsBufferAddress = boundsBuffer.GetAddress(),
                TotalInvocations = renderContext.TotalMeshCount,
                OutputBufferAddress = outputBuffer.GetAddress()
            })
            .Invoke((uint)renderContext.TotalMeshCount);
    }

    [NoReorder]
    private struct Push
    {
        public required ulong BoundsBufferAddress;
        public required int TotalInvocations;
        public required ulong OutputBufferAddress;
    }
}
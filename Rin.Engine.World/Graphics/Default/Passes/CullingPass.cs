using JetBrains.Annotations;
using Rin.Framework.Graphics;
using Rin.Framework.Graphics.Graph;
using Rin.Framework.Graphics.Shaders;

namespace Rin.Engine.World.Graphics.Default.Passes;

/// <summary>
///     Culls all meshes based on the main view (Will be updated to support a view index in the future)
///     writes results to <see cref="CullingPass.OutputBufferId" />
/// </summary>
/// <param name="renderContext"></param>
public class CullingPass(DefaultWorldRenderContext renderContext) : IComputePass
{
    private readonly IComputeShader _shader = SGraphicsModule
        .Get()
        .MakeCompute("World/Shaders/Mesh/Compute/culling.slang");

    [PublicAPI] public uint OutputBufferId { get; set; }

    public uint Id { get; set; }
    public bool IsTerminal => false;

    public void Configure(IGraphConfig config)
    {
        config.ReadBuffer(renderContext.BoundsBufferId, GraphBufferUsage.Compute);
        OutputBufferId = config.CreateBuffer<uint>(renderContext.TotalMeshCount, GraphBufferUsage.Compute);
    }

    public void Execute(ICompiledGraph graph, IExecutionContext ctx)
    {
        var boundsBuffer = graph.GetBufferOrException(renderContext.BoundsBufferId);
        var outputBuffer = graph.GetBufferOrException(OutputBufferId);

        if (_shader.Bind(ctx) is not {} bindContext) return;
        bindContext
            .Push(new Push
            {
                BoundsBufferAddress = boundsBuffer.GetAddress(),
                TotalInvocations = renderContext.TotalMeshCount,
                OutputBufferAddress = outputBuffer.GetAddress()
            })
            .Invoke((uint)renderContext.TotalMeshCount);
    }


    private struct Push
    {
        public required ulong BoundsBufferAddress;
        public required int TotalInvocations;
        public required ulong OutputBufferAddress;
    }
}
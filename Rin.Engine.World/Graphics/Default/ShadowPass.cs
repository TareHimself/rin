using Rin.Engine.Graphics;
using Rin.Engine.Graphics.FrameGraph;

namespace Rin.Engine.World.Graphics.Default;

public class ShadowPass(DefaultWorldRenderContext renderContext) : IPass
{
    public uint Id { get; set; }
    public bool IsTerminal => false;

    public void Configure(IGraphConfig config)
    {
    }

    public void Execute(ICompiledGraph graph, IExecutionContext ctx)
    {
        throw new NotImplementedException();
    }
}
using Rin.Engine.Graphics;
using Rin.Engine.Graphics.FrameGraph;

namespace Rin.Engine.World.Graphics;

public class ShadowPass(WorldContext worldContext) : IPass
{
    public uint Id { get; set; }
    public bool IsTerminal => false;
    public bool HandlesPreAdd => false;
    public bool HandlesPostAdd => false;
    public void PreAdd(IGraphBuilder builder)
    {
        throw new NotImplementedException();
    }

    public void PostAdd(IGraphBuilder builder)
    {
        throw new NotImplementedException();
    }

    public void Configure(IGraphConfig config)
    {
        
    }

    public void Execute(ICompiledGraph graph, IExecutionContext ctx)
    {
        throw new NotImplementedException();
    }
}
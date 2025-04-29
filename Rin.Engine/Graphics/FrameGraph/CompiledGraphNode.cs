namespace Rin.Engine.Graphics.FrameGraph;

public class CompiledGraphNode : ICompiledGraphNode
{
    public required IPass Pass { get; init; }
    public required HashSet<IPass> Dependencies { get; init; }
}
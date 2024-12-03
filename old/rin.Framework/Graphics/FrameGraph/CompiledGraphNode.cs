namespace rin.Framework.Graphics.FrameGraph;

public struct CompiledGraphNode : ICompiledGraphNode
{
    public required IPass Pass { get; init; }
    public required HashSet<IPass> Dependents { get; init; }
    public required HashSet<IPass> Dependencies { get; init; }
}
namespace rin.Graphics.FrameGraph;

public struct CompiledGraphNode : ICompiledGraphNode
{
    public required IPass Pass { get; set; }
    public required HashSet<IPass> Dependents { get; set; }
    public required HashSet<IPass> Dependencies { get; set; }
}
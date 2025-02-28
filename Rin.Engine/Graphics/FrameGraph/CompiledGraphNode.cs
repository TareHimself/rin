namespace Rin.Engine.Graphics.FrameGraph;

public struct CompiledGraphNode : ICompiledGraphNode
{
    public required IPass Pass { get; init; }
    public required HashSet<IPass> Dependencies { get; init; }

    public required ulong MemoryRequired { get; set; }
}
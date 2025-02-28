namespace Rin.Engine.Graphics.FrameGraph;

public interface ICompiledGraphNode
{
    public IPass Pass { get; }
    public HashSet<IPass> Dependencies { get; }
}
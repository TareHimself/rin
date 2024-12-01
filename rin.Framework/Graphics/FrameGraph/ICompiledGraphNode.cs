namespace rin.Framework.Graphics.FrameGraph;

public interface ICompiledGraphNode
{
    public IPass Pass { get; }
    public HashSet<IPass> Dependents { get; }
    public HashSet<IPass> Dependencies { get; }
}
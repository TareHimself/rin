namespace rin.Graphics.FrameGraph;

public interface ICompiledGraph : IDisposable
{
    public abstract IGraphResource GetResource(string id);
    
    public abstract void Run(Frame frame);
}
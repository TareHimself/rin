namespace rin.Graphics.FrameGraph;

public interface ICompiledGraph : IDisposable
{
    public abstract IGraphResource GetResource(IResourceHandle handle);
    
    public abstract void Run(Frame frame);
}
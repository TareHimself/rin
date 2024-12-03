namespace rin.Framework.Graphics.FrameGraph;

public interface ICompiledGraph : IDisposable
{
    public IGraphResource GetResource(string id);
    
    public void Run(Frame frame);
}
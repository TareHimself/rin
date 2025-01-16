namespace rin.Framework.Graphics.FrameGraph;

public interface ICompiledGraph : IDisposable
{
    public IGraphResource GetResource(uint id);
    
    public void Execute(Frame frame);
}
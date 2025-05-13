namespace Rin.Engine.Graphics.FrameGraph;

public interface ICompiledGraph : IDisposable
{
    public IGraphImage GetImage(uint id);

    public IDeviceBufferView GetBuffer(uint id);

    public void Execute(Frame frame, IRenderContext context, TaskPool taskPool);
}
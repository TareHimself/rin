namespace Rin.Engine.Graphics.FrameGraph;

public interface ICompiledGraph : IDisposable
{
    public IGraphImage GetImage(uint id);

    public DeviceBufferView GetBuffer(uint id);

    public void Execute(Frame frame, IRenderData context, TaskPool taskPool);
}
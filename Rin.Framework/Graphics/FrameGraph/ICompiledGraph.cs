namespace Rin.Framework.Graphics.FrameGraph;

public interface ICompiledGraph : IDisposable
{
    public IGraphImage GetImage(uint id);

    public DeviceBufferView GetBuffer(uint id);

    public void Execute(IExecutionContext context);
}
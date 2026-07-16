namespace Rin.Core.Graphics.Graph;

public interface ICompiledGraph : IDisposable
{
    public ResourceHandle GetImage(uint id);

    public DeviceBufferView GetBuffer(uint id);

    public void Execute(IExecutionContext context);
}
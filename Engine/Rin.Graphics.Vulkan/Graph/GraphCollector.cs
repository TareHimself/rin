using Rin.Core.Graphics.Graph;

namespace Rin.Graphics.Vulkan.Graph;

public class GraphCollector : IGraphCollector, IDisposable
{
    private readonly List<ICollectedData> _data = [];

    public void Add(ICollectedData data)
    {
        _data.Add(data);
    }

    public void Write(IGraphBuilder builder)
    {
        foreach (var data in _data) data.Write(builder);
    }

    public void Dispose()
    {
        foreach (var data in _data) (data as IDisposable)?.Dispose();
    }
}
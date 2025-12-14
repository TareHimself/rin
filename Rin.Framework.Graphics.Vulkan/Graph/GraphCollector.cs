using Rin.Framework.Graphics.Graph;

namespace Rin.Framework.Graphics.Vulkan.Graph;

public class GraphCollector : IGraphCollector
{
    private List<ICollectedData> _data = [];
    
    public void Add(ICollectedData data)
    {
        _data.Add(data);
    }

    public void Write(IGraphBuilder builder)
    {
        foreach (var data in _data)
        {
            data.Write(builder);
        }
    }
}
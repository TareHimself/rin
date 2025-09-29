namespace Rin.Framework.Graphics.Graph;

public interface IGraphCollector
{
    /// <summary>
    /// Add collected data
    /// </summary>
    /// <param name="data"></param>
    public void Add(ICollectedData data);
    
    /// <summary>
    /// writes passes to the <see cref="IGraphBuilder"/> using the current collected data
    /// </summary>
    /// <param name="builder"></param>
    /// <returns></returns>
    public void Write(IGraphBuilder builder);
}
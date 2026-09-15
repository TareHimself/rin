namespace Rin.Core.Graphics.Graph;

public interface IGraphCollector
{
    /// <summary>
    ///     Add collected data
    /// </summary>
    /// <param name="data"></param>
    public void Add(ICollectedData data);
}
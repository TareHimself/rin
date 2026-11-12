namespace Rin.Core.Graphics.Graph;

public interface IGraphCollector
{
    /// <summary>
    ///     Add collected data
    /// </summary>
    public void Add(ICollectedData data);
}
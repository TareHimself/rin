namespace Rin.Core.Graphics.Graph;

public interface ICollectedData
{
    /// <summary>
    ///     Convert the collected data into <see cref="IPass" /> for the graph
    /// </summary>
    public void Write(IGraphBuilder builder);
}
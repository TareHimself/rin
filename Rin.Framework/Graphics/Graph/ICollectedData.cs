namespace Rin.Framework.Graphics.Graph;

public interface ICollectedData
{
    /// <summary>
    /// Convert the collected data into <see cref="IPass"/> for the graph
    /// </summary>
    /// <param name="builder"></param>
    public void Write(IGraphBuilder builder);
}
namespace Rin.Engine.Graphics.FrameGraph;

/// <summary>
///     Allows an <see cref="IPass" /> to implement <see cref="PreAdd" />
/// </summary>
public interface IWithPreAdd : IPass
{
    /// <summary>
    ///     Called just before this pass is added to the builder  <see cref="builder" />
    /// </summary>
    public void PreAdd(IGraphBuilder builder);
}
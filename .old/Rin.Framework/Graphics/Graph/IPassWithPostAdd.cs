namespace Rin.Framework.Graphics.Graph;

/// <summary>
///     Allows an <see cref="IPass" /> to implement <see cref="PostAdd" />
/// </summary>
public interface IPassWithPostAdd : IPass
{
    /// <summary>
    ///     Called just after this pass is added to the <see cref="builder" />
    /// </summary>
    public void PostAdd(IGraphBuilder builder);
}
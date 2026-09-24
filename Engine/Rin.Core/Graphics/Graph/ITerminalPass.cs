namespace Rin.Core.Graphics.Graph;

/// <summary>
///     Marks an <see cref="IPass" /> as a valid endpoint of the frame graph, used for pruning, if a
///     <see cref="IGraphBuilder" /> has no terminal passes, nothing will be drawn
/// </summary>
public interface ITerminalPass : IPass;

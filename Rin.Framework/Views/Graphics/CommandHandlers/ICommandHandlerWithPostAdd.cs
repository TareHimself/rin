using Rin.Framework.Graphics.FrameGraph;

namespace Rin.Framework.Views.Graphics.CommandHandlers;

/// <summary>
///     Responsible for executing commands in a pass
/// </summary>
public interface ICommandHandlerWithPostAdd : ICommandHandler
{
    public void PostAdd(IGraphBuilder builder);
}
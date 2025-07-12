using Rin.Engine.Graphics;
using Rin.Engine.Graphics.FrameGraph;
using Rin.Engine.Views.Graphics.Commands;

namespace Rin.Engine.Views.Graphics.CommandHandlers;

/// <summary>
/// Responsible for executing commands in a pass
/// </summary>
public interface ICommandHandlerWithPreAdd : ICommandHandler
{
    public void PreAdd(IGraphBuilder builder);
}
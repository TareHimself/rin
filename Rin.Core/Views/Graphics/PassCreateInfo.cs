using Rin.Core.Views.Graphics.CommandHandlers;

namespace Rin.Core.Views.Graphics;

public class PassCreateInfo(SurfaceContext context, ICommandHandler[] handlers)
{
    public SurfaceContext Context { get; } = context;
    public ICommandHandler[] Handlers { get; } = handlers;
}
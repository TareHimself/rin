using Rin.Framework.Views.Graphics.CommandHandlers;

namespace Rin.Framework.Views.Graphics;

public class PassCreateInfo(SurfaceContext context, ICommandHandler[] handlers)
{
    public SurfaceContext Context { get; } = context;
    public ICommandHandler[] Handlers { get; } = handlers;
}
using Rin.Engine.Views.Graphics.CommandHandlers;
using Rin.Engine.Views.Graphics.Commands;

namespace Rin.Engine.Views.Graphics;

public class PassCreateInfo(SurfacePassContext context, ICommandHandler[] handlers)
{
    public SurfacePassContext Context { get; } = context;
    public ICommandHandler[] Handlers { get; } = handlers;
}
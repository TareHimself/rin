using Rin.Engine.Views.Graphics.Commands;

namespace Rin.Engine.Views.Graphics;

public class PassCreateInfo(SurfacePassContext context, ICommand[] commands)
{
    public SurfacePassContext Context { get; } = context;
    public ICommand[] Commands { get; } = commands;
}
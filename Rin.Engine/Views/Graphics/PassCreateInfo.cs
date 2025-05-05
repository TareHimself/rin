using Rin.Engine.Views.Graphics.Commands;

namespace Rin.Engine.Views.Graphics;

public class PassCreateInfo(SharedPassContext context, ICommand[] commands)
{
    public SharedPassContext Context { get; } = context;
    public ICommand[] Commands { get; } = commands;
}
namespace rin.Framework.Views.Graphics.Commands;

/// <summary>
/// Base class for commands that run before or after the main pass
/// </summary>
public abstract class UtilityCommand : Command
{
    public virtual CommandStage Stage => CommandStage.Before;
    public abstract void Execute(ViewsFrame frame);
}
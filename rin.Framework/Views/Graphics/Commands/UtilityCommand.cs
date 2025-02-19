using JetBrains.Annotations;
using rin.Framework.Graphics.FrameGraph;

namespace rin.Framework.Views.Graphics.Commands;

/// <summary>
///     Base class for commands that run before or after the main pass
/// </summary>
public abstract class UtilityCommand : Command
{
    public virtual CommandStage Stage => CommandStage.Before;

    /// <summary>
    ///     Called before the <see cref="ViewsPass" /> is added to the <see cref="IGraphBuilder" />
    /// </summary>
    [PublicAPI]
    public abstract void BeforeAdd(IGraphBuilder builder);

    /// <summary>
    ///     Configure the <see cref="ViewsPass" /> running this utility command
    /// </summary>
    /// <param name="config"></param>
    [PublicAPI]
    public abstract void Configure(IGraphConfig config);

    public abstract void Execute(ViewsFrame frame);
}
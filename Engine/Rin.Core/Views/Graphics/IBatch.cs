using Rin.Core.Graphics.Graph;
using Rin.Core.Views.Graphics.Commands;

namespace Rin.Core.Views.Graphics;

public interface IBatch
{
    ulong GetMemoryNeeded();
    IBatcher GetBatcher();
    void AddFromCommand(ICommand command);

    /// <summary>
    ///     Registers and reads every graph resource this batch's draws use (e.g. textures), so they stay alive
    ///     until the frame has rendered.
    /// </summary>
    void DeclareResources(IGraphConfig config);
}
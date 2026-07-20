using Rin.Core.Views.Graphics.Commands;

namespace Rin.Core.Views.Graphics;

public interface IBatch
{
    ulong GetMemoryNeeded();
    IBatcher GetBatcher();
    void AddFromCommand(ICommand command);
}
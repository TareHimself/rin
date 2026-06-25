using Rin.Core.Views.Graphics.Commands;

namespace Rin.Core.Views.Graphics;

public interface IBatch
{
    IEnumerable<ulong> GetMemoryNeeded();
    IBatcher GetBatcher();
    void AddFromCommand(ICommand command);
}